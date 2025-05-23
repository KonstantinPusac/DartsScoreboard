using DartsScoreboard.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static MudBlazor.CategoryTypes;

namespace DartsScoreboard
{
    public class PlayerScoreInfo
    {
        public int PlayerScore { get; set; }
        public int PlayerThrows { get; set; }
        public int PlayerThrowsLeg { get; set; }
        public int PlayerSets { get; set; }
        public int PlayerLegs { get; set; }
        public int PlayerCollectedScore { get; set; }
    }
    public class RoundSnapshot
    {
        public int CurrentPlayerIndex { get; set; }
        public string InputScore { get; set; } = "";
        public Dictionary<int, PlayerScoreInfo> PlayerStates { get; set; } = new();
        public List<User> PlayerStatsSnapshot { get; set; } = new();
    }
    public partial class GameStandardPlay
    {
        // Unique code per game
        [Parameter] public string GameCode { get; set; } = "";

        [Inject] NavigationManager NavManager { get; set; } = default!;
        [Inject] public PlayerSelectionService PlayerService { get; set; } = default!;
        [Inject] public GameSettingsService GameSettings { get; set; } = default!;
        [Inject] public IStandardGamePersistence _StandardGamePersistence { get; set; }
        [Inject] public IUserPersistence _UserPersistence { get; set; }
        [Inject] ISnackbar Snackbar { get; set; } = default!;

        // Player info
        public List<User> Players { get; set; } = new();
        public Dictionary<int, PlayerScoreInfo> PlayerScores { get; set; } = new();
        public int CurrentPlayerIndex { get; set; } = 0;

        // Input scores
        public string InputScore { get; set; } = "";
        public string InputScoreDartOne { get; set; } = "";
        public string InputScoreDartTwo { get; set; } = "";
        public string InputScoreDartThree { get; set; } = "";
        public int DartIndex { get; set; } = 1; // 1 = first, 2 = second, 3 = third

        // Keyboard selection
        public bool UseThreeDartMode { get; set; } = false;
        public string SelectedMultiplier { get; set; } = "S"; // "S", "D", or "T"
            
        // Winner popup
        public bool WinnerPopup { get; set; } = false;

        // Player rotation
        private int StartingPlayerIndexSets = 0;
        private int StartingPlayerIndexLegs = 0;

        // Default game settings
        public int StartingScore = 501;
        public string StartingIn = "STRAIGHT IN";
        public string StartingOut = "DOUBLE OUT";
        public int NumOfSets = 1; // Number of sets per leg
        public int NumOfLegs = 1; // Number of legs to play

        // Creating undo stack
        public Stack<RoundSnapshot> UndoStack = new();

        // Checkout list and darts used on a double
        private void OnSelectDartsUsedOnDouble(int value) => SelectedDartsUsedOnDouble = value;
        private void OnSelectDartsUsedOnCheckout(int value) => SelectedDartsUsedOnCheckout = value;
        private void CloseCheckoutPopup()
        {
            ShowCheckoutPopup = false;
            InputScore = "";
        }

        public int SelectedDartsUsedOnDouble { get; set; } = 1;
        public int SelectedDartsUsedOnCheckout { get; set; } = 3;

        public bool ShowCheckoutPopup { get; set; } = false;
        public List<int> AvailableDoubleDartOptions { get; set; } = new();
        public List<int> AvailableCheckoutDartOptions { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(GameCode))
            {
                var savedGame = await _StandardGamePersistence.Get(GameCode);
                if (savedGame != null)
                {
                    UndoStack = new Stack<RoundSnapshot>(savedGame.UndoHistory);

                    // Restore game state
                    GameCode = savedGame.Code;
                    Players = savedGame.Players;
                    PlayerScores = savedGame.PlayerScores;
                    CurrentPlayerIndex = savedGame.CurrentPlayerIndex;
                    DartIndex = savedGame.DartIndex;

                    InputScoreDartOne = savedGame.InputScoreDartOne;
                    InputScoreDartTwo = savedGame.InputScoreDartTwo;
                    InputScoreDartThree = savedGame.InputScoreDartThree;
                    UseThreeDartMode = savedGame.UseThreeDartMode;
                    SelectedMultiplier = savedGame.SelectedMultiplier;
                    WinnerPopup = savedGame.WinnerPopup;

                    StartingScore = savedGame.StartingPoints;
                    StartingIn = savedGame.StartingIn;
                    StartingOut = savedGame.StartingOut;
                    NumOfLegs = savedGame.NumOfLegs;
                    NumOfSets = savedGame.NumOfSets;

                    PlayerStatsToPlayers(savedGame.PlayerStats);
                    return;
                }
            }

            // Fallback: Start new game
            GameCode = GameCode == "" ? Guid.NewGuid().ToString() : GameCode;

            StartingScore = GameSettings.StartingScore;
            StartingIn = GameSettings.StartInOption;
            StartingOut = GameSettings.EndInOption;
            NumOfLegs = GameSettings.Legs;
            NumOfSets = GameSettings.Sets;

            foreach (var player in Players)
            {
                PlayerScores[player.Id] = new PlayerScoreInfo
                {
                    PlayerScore = StartingScore,
                    PlayerThrows = 0,
                    PlayerThrowsLeg = 0,
                    PlayerSets = 0,
                    PlayerLegs = 0,
                    PlayerCollectedScore = 0
                };
            }
        }
        private string GetPlayerClass(User player)
        {
            if (Players == null || Players.Count == 0 || CurrentPlayerIndex >= Players.Count)
                return "pa-2";
            return player == Players[CurrentPlayerIndex] ? "pa-2 mud-theme-primary" : "pa-2";
        }

        private string GetSuggestedOut()
        {
            if (Players == null || Players.Count == 0 || CurrentPlayerIndex >= Players.Count)
                return "";

            var score = GetDisplayedScore(Players[CurrentPlayerIndex].Id).ToString();
            if (SuggestionFinishes.TryGetValue(score, out var suggestion))
                return string.Join(" ", suggestion);
            return "No outs";
        }

        private string GetAverage()
        {
            if (Players == null || Players.Count == 0 || CurrentPlayerIndex >= Players.Count)
                return "0.00";
            return Players[CurrentPlayerIndex].Stats.ThreeDartAverage.ToString("0.00");
        }

        private string GetLegAverage()
        {
            if (Players == null || Players.Count == 0 || CurrentPlayerIndex >= Players.Count)
                return "0.00";
            return Players[CurrentPlayerIndex].Stats.ThreeDartLegAverage.ToString("0.00");
        }

        private int GetDartsThrown()
        {
            if (Players == null || Players.Count == 0 || CurrentPlayerIndex >= Players.Count)
                return 0;
            return PlayerScores[Players[CurrentPlayerIndex].Id].PlayerThrows;
        }

        private void PlayerStatsToPlayers(List<User> savedStats)
        {
            foreach (var stat in savedStats)
            {
                var player = Players.FirstOrDefault(p => p.Id == stat.Id);
                if (player != null)
                {
                    player.Stats = stat.Stats;
                }
            }
        }

        private void SwitchToRegularMode()
        {
            UseThreeDartMode = false;
            InputScoreDartOne = "";
            InputScoreDartTwo = "";
            InputScoreDartThree = "";
            DartIndex = 1;
        }
        private void SwitchToThreeDartMode()
        {
            UseThreeDartMode = true;
            InputScore = "";
            DartIndex = 1;
        }

        public int GetDisplayedScore(int playerId)
        {
            // Get base score
            int baseScore = PlayerScores[playerId].PlayerScore;

            int liveScore = baseScore; // Default to base score
            // Only subtract darts for the current player
            if (Players[CurrentPlayerIndex].Id != playerId)
                return baseScore;

            if (UseThreeDartMode)
            {
                int dartOne = int.TryParse(InputScoreDartOne, out var v1) ? v1 : 0;
                int dartTwo = int.TryParse(InputScoreDartTwo, out var v2) ? v2 : 0;
                int dartThree = int.TryParse(InputScoreDartThree, out var v3) ? v3 : 0;
                int subtract = dartOne + dartTwo + dartThree;

                liveScore = baseScore - subtract;

                if (liveScore < 0 || liveScore == 1)
                {
                    // Player has busted
                    liveScore = baseScore;
                }
            }

            return liveScore;
        }
        private void GoHome()
        {
            NavManager.NavigateTo("/");
        }

        private int CombineScore()
        {
            int dartOne = int.TryParse(InputScoreDartOne, out int dartOneValue) ? dartOneValue : 0;
            int dartTwo = int.TryParse(InputScoreDartTwo, out int dartTwoValue) ? dartTwoValue : 0;
            int dartThree = int.TryParse(InputScoreDartThree, out int dartThreeValue) ? dartThreeValue : 0;
            int totalScore = dartOne + dartTwo + dartThree;

            return totalScore;
        }

        // Numbers that cannot be finished
        private static readonly HashSet<int> NoFinishScores = new() { 169, 168, 166, 165, 163, 162, 159 };
        private bool NoScore()
        {
            var currentPlayer = Players[CurrentPlayerIndex];
            return NoFinishScores.Contains(PlayerScores[currentPlayer.Id].PlayerScore);
        }
        private void DoubleOutCheckout(int score)
        {
            var currentPlayer = Players[CurrentPlayerIndex];

            if (score > 180 || PlayerScores[currentPlayer.Id].PlayerScore - score < 0)
            {
                // Invalid score
                InputScore = "";
                return;
            }

            if (NoScore())
            {
                PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;
            }
            else if (PlayerScores[currentPlayer.Id].PlayerScore > 100 || PlayerScores[currentPlayer.Id].PlayerScore == 99)
            {
                PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;
                if (PlayerScores[currentPlayer.Id].PlayerScore - score < 51 && PlayerScores[currentPlayer.Id].PlayerScore - score > 1)
                {
                    // Setup the popup
                    AvailableDoubleDartOptions = new List<int> { 0, 1 };
                    SelectedDartsUsedOnDouble = AvailableDoubleDartOptions.First();

                    AvailableCheckoutDartOptions = new();
                    ShowCheckoutPopup = true;
                }
                else if (PlayerScores[currentPlayer.Id].PlayerScore - score == 0)
                {
                    currentPlayer.Stats.NumOfDoublesThrown += 1;
                }
            }
            else if (PlayerScores[currentPlayer.Id].PlayerScore > 40 || PlayerScores[currentPlayer.Id].PlayerScore % 2 != 0 && PlayerScores[currentPlayer.Id].PlayerScore > 1 ||
                !(PlayerScores[currentPlayer.Id].PlayerScore != 101 && PlayerScores[currentPlayer.Id].PlayerScore != 104 && PlayerScores[currentPlayer.Id].PlayerScore != 107 && PlayerScores[currentPlayer.Id].PlayerScore != 110))
            {
                if (PlayerScores[currentPlayer.Id].PlayerScore - score == 0)
                {
                    AvailableDoubleDartOptions = new List<int> { 1, 2 };
                    SelectedDartsUsedOnDouble = AvailableDoubleDartOptions.First();

                    if (UseThreeDartMode)
                    {
                        if (InputScoreDartThree != "")
                        {
                            PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                            PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;
                        }
                        else
                        {
                            PlayerScores[currentPlayer.Id].PlayerThrows += 2;
                            PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 2;
                        }
                    }
                    else
                    {
                        AvailableCheckoutDartOptions = new List<int> { 2, 3 };
                        SelectedDartsUsedOnCheckout = AvailableCheckoutDartOptions.First();
                    }
                    ShowCheckoutPopup = true;
                }
                else if (PlayerScores[currentPlayer.Id].PlayerScore - score > 50)
                {
                    PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                    PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;
                }
                else if (PlayerScores[currentPlayer.Id].PlayerScore - score < 51)
                {
                    // Setup the popup
                    PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                    PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;

                    AvailableDoubleDartOptions = new List<int> { 0, 1, 2 };
                    SelectedDartsUsedOnDouble = AvailableDoubleDartOptions.First();

                    AvailableCheckoutDartOptions = new();
                    ShowCheckoutPopup = true;
                }
            }
            else if (PlayerScores[currentPlayer.Id].PlayerScore > 1)
            {
                if (PlayerScores[currentPlayer.Id].PlayerScore - score == 0)
                {
                    AvailableDoubleDartOptions = new List<int> { 1, 2, 3 };
                    SelectedDartsUsedOnDouble = AvailableDoubleDartOptions.First();

                    if (UseThreeDartMode)
                    {
                        if (InputScoreDartThree != "")
                        {
                            PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                            PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;
                        }
                        else if (InputScoreDartTwo != "")
                        {
                            PlayerScores[currentPlayer.Id].PlayerThrows += 2;
                            PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 2;
                        }
                        else
                        {
                            PlayerScores[currentPlayer.Id].PlayerThrows += 1;
                            PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 1;
                        }
                    }
                    else
                    {
                        AvailableCheckoutDartOptions = new List<int> { 1, 2, 3 };
                        SelectedDartsUsedOnCheckout = AvailableCheckoutDartOptions.First();
                    }
                    ShowCheckoutPopup = true;
                }
                else if (PlayerScores[currentPlayer.Id].PlayerScore - score > 1)
                {
                    PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                    PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;

                    AvailableDoubleDartOptions = new List<int> { 0, 1, 2, 3 };
                    SelectedDartsUsedOnDouble = AvailableDoubleDartOptions.First();

                    AvailableCheckoutDartOptions = new();
                    ShowCheckoutPopup = true;
                }
            }
            else
            {
                // Invalid score
                InputScore = "";
                return; // ERROR: No valid checkout
            }

            // Three dart leg average
            currentPlayer.Stats.ThreeDartLegAverage = (double)((StartingScore - PlayerScores[currentPlayer.Id].PlayerScore) / ((double)PlayerScores[currentPlayer.Id].PlayerThrowsLeg / 3));

            // Three dart average
            PlayerScores[currentPlayer.Id].PlayerCollectedScore += score;
            currentPlayer.Stats.ThreeDartAverage = (double)(PlayerScores[currentPlayer.Id].PlayerCollectedScore / ((double)PlayerScores[currentPlayer.Id].PlayerThrows / 3));

            // Checkout percentage
            if (currentPlayer.Stats.NumOfDoublesThrown > 0)
                currentPlayer.Stats.CheckoutPercentage = (PlayerScores[currentPlayer.Id].PlayerLegs + NumOfSets * NumOfLegs) / (double)currentPlayer.Stats.NumOfDoublesThrown * 100;
            else
                currentPlayer.Stats.CheckoutPercentage = 0;
        }
        private async Task ConfirmCheckoutData()
        {

            if (int.TryParse(InputScore, out int score))
            {
                var currentPlayer = Players[CurrentPlayerIndex];

                currentPlayer.Stats.NumOfDoublesThrown += SelectedDartsUsedOnDouble;

                if (AvailableCheckoutDartOptions?.Count > 0)
                {
                    // Only when you actually offered Checkout Options
                    PlayerScores[currentPlayer.Id].PlayerThrows += SelectedDartsUsedOnCheckout;
                    PlayerScores[currentPlayer.Id].PlayerThrowsLeg += SelectedDartsUsedOnCheckout;
                }

                // Close popup
                ShowCheckoutPopup = false;
                SelectedDartsUsedOnDouble = 0;
                SelectedDartsUsedOnCheckout = 0;

                await SubmintigScore(score);
            }
        }
        private async Task SubmitScore()
        {
            var currentPlayer = Players[CurrentPlayerIndex];

            // Three dart keyboard conditions
            SelectedMultiplier = "S";
            if (UseThreeDartMode)
            {
                InputScore = CombineScore().ToString();
            }

            if (InputScore == "")
            {
                InputScore = "0";
            }

            if (int.TryParse(InputScore, out int score))
            {
                // Pushing stats and info to undo stack
                if (score < 181)
                {
                    var snapshot = new RoundSnapshot
                    {
                        CurrentPlayerIndex = CurrentPlayerIndex,
                        InputScore = InputScore,
                        PlayerStates = Players.ToDictionary(p => p.Id, p => new PlayerScoreInfo
                        {
                            PlayerScore = PlayerScores[p.Id].PlayerScore,
                            PlayerThrows = PlayerScores[p.Id].PlayerThrows,
                            PlayerThrowsLeg = PlayerScores[p.Id].PlayerThrowsLeg,
                            PlayerLegs = PlayerScores[p.Id].PlayerLegs,
                            PlayerSets = PlayerScores[p.Id].PlayerSets,
                            PlayerCollectedScore = PlayerScores[p.Id].PlayerCollectedScore
                        }),
                        PlayerStatsSnapshot = Players.Select(p => new User
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Stats = new UserStats
                            {
                                ThreeDartAverage = p.Stats.ThreeDartAverage,
                                CheckoutPercentage = p.Stats.CheckoutPercentage,
                                ThreeDartLegAverage = p.Stats.ThreeDartLegAverage,
                                HighScoreHits = new Dictionary<string, int>(p.Stats.HighScoreHits)
                            }
                        }).ToList()
                    };
                    UndoStack.Push(snapshot);


                    // Calculating averages
                    if (PlayerScores[currentPlayer.Id].PlayerScore > 170 || StartingOut != "DOUBLE OUT")
                    {
                        PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                        PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;

                        // Three dart leg average
                        currentPlayer.Stats.ThreeDartLegAverage = (double)((StartingScore - PlayerScores[currentPlayer.Id].PlayerScore) / ((double)PlayerScores[currentPlayer.Id].PlayerThrowsLeg / 3));

                        // Three dart average
                        PlayerScores[currentPlayer.Id].PlayerCollectedScore += score;
                        currentPlayer.Stats.ThreeDartAverage = (double)(PlayerScores[currentPlayer.Id].PlayerCollectedScore / ((double)PlayerScores[currentPlayer.Id].PlayerThrows / 3));

                        // Checkout percentage
                        if (currentPlayer.Stats.NumOfDoublesThrown > 0)
                            currentPlayer.Stats.CheckoutPercentage = (PlayerScores[currentPlayer.Id].PlayerLegs + NumOfSets * NumOfLegs) / (double)currentPlayer.Stats.NumOfDoublesThrown * 100;
                        else
                            currentPlayer.Stats.CheckoutPercentage = 0;
                    }
                    else
                    {
                        if (NoScore())
                        {
                            if (PlayerScores[currentPlayer.Id].PlayerScore - score == 0)
                            {
                                // Invalid score
                                InputScore = "";
                                return;
                            }
                        }

                        DoubleOutCheckout(score);
                    }

                    if (!ShowCheckoutPopup)
                    {
                        await SubmintigScore(score);
                    }
                }
                else
                {
                    // Invalid score 
                    Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
                    Snackbar.Add("Invalid score", Severity.Error, options => 
                    {
                        options.RequireInteraction = false;
                        options.HideIcon = false;
                        options.VisibleStateDuration = 500;
                    });
                    InputScore = "";
                    return;
                }
            }
        }
        private async Task SubmintigScore(int score)
        {
            DartIndex = 1; // wrap around or submit here

            var currentPlayer = Players[CurrentPlayerIndex];

            if (score > 180 || PlayerScores[currentPlayer.Id].PlayerScore - score < 0)
            {
                // Invalid score 
                Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
                Snackbar.Add("Invalid score", Severity.Error, options =>
                {
                    options.RequireInteraction = false;
                    options.HideIcon = false;
                    options.VisibleStateDuration = 500;
                });
                InputScore = "";
                return;
            }

            PlayerScores[currentPlayer.Id].PlayerScore -= score;
            UpdateHighScoreHits(currentPlayer, score);

            // Loading other player scores
            foreach (var player in Players)
            {
                if (PlayerScores[player.Id].PlayerThrows > 0 && PlayerScores[player.Id].PlayerThrowsLeg > 0)
                {
                    // Three dart leg average
                    currentPlayer.Stats.ThreeDartLegAverage = (double)((StartingScore - PlayerScores[currentPlayer.Id].PlayerScore) / ((double)PlayerScores[currentPlayer.Id].PlayerThrowsLeg / 3));

                    // Three dart average
                    player.Stats.ThreeDartAverage = (double)(PlayerScores[player.Id].PlayerCollectedScore / ((double)PlayerScores[player.Id].PlayerThrows / 3));
                }
                else
                {
                    player.Stats.ThreeDartAverage = 0;
                    player.Stats.ThreeDartLegAverage = 0;
                }

                if (player.Stats.NumOfDoublesThrown > 0)
                    player.Stats.CheckoutPercentage = (PlayerScores[player.Id].PlayerLegs + NumOfSets * NumOfLegs) / (double)player.Stats.NumOfDoublesThrown * 100;
                else
                    player.Stats.CheckoutPercentage = 0;
            }

            if (PlayerScores[currentPlayer.Id].PlayerScore == 0)
            {
                if (PlayerScores[currentPlayer.Id].PlayerLegs + 1 == NumOfLegs)
                {
                    PlayerScores[currentPlayer.Id].PlayerSets++;
                    // Darts per leg
                    currentPlayer.Stats.DartsPerLeg = (double)(PlayerScores[currentPlayer.Id].PlayerThrowsLeg / (double)NumOfLegs);
                    currentPlayer.Stats.TotalDartsThrown += PlayerScores[currentPlayer.Id].PlayerThrowsLeg;

                    // Check best number of darts thrown leg
                    if (currentPlayer.Stats.BestNumOfDartsThrown == 0 || currentPlayer.Stats.BestNumOfDartsThrown > PlayerScores[currentPlayer.Id].PlayerThrowsLeg)
                    {
                        currentPlayer.Stats.BestNumOfDartsThrown = PlayerScores[currentPlayer.Id].PlayerThrowsLeg;
                    }

                    // Check worst number of darts thrown leg
                    if (currentPlayer.Stats.WorstNumOfDartsThrown == 0 || currentPlayer.Stats.WorstNumOfDartsThrown < PlayerScores[currentPlayer.Id].PlayerThrowsLeg)
                    {
                        currentPlayer.Stats.WorstNumOfDartsThrown = PlayerScores[currentPlayer.Id].PlayerThrowsLeg;
                    }

                    // Reset player scores
                    foreach (var players in Players)
                    {
                        PlayerScores[players.Id].PlayerLegs = 0;
                        PlayerScores[players.Id].PlayerScore = GameSettings.StartingScore;
                        PlayerScores[players.Id].PlayerThrowsLeg = 0;
                    }

                    // Rotate starting player for next leg/set
                    StartingPlayerIndexSets = (StartingPlayerIndexSets + 1) % Players.Count;
                    CurrentPlayerIndex = StartingPlayerIndexSets;

                    // Check best three dart leg average
                    if (currentPlayer.Stats.BestThreeDartLegAverage < currentPlayer.Stats.ThreeDartLegAverage)
                    {
                        currentPlayer.Stats.BestThreeDartLegAverage = currentPlayer.Stats.ThreeDartLegAverage;
                    }
                }
                else
                {
                    PlayerScores[currentPlayer.Id].PlayerLegs++;
                    // Darts per leg
                    currentPlayer.Stats.DartsPerLeg = (double)(PlayerScores[currentPlayer.Id].PlayerThrowsLeg / (double)NumOfLegs);
                    currentPlayer.Stats.TotalDartsThrown += PlayerScores[currentPlayer.Id].PlayerThrowsLeg;

                    // Check best number of darts thrown leg
                    if (currentPlayer.Stats.BestNumOfDartsThrown == 0 || currentPlayer.Stats.BestNumOfDartsThrown > PlayerScores[currentPlayer.Id].PlayerThrowsLeg)
                    {
                        currentPlayer.Stats.BestNumOfDartsThrown = PlayerScores[currentPlayer.Id].PlayerThrowsLeg;
                    }

                    // Check worst number of darts thrown leg
                    if (currentPlayer.Stats.WorstNumOfDartsThrown == 0 || currentPlayer.Stats.WorstNumOfDartsThrown < PlayerScores[currentPlayer.Id].PlayerThrowsLeg)
                    {
                        currentPlayer.Stats.WorstNumOfDartsThrown = PlayerScores[currentPlayer.Id].PlayerThrowsLeg;
                    }

                    // Reset player scores
                    foreach (var players in Players)
                    {
                        PlayerScores[players.Id].PlayerScore = GameSettings.StartingScore;
                        PlayerScores[players.Id].PlayerThrowsLeg = 0;
                    }

                    // Rotate starting player for next leg/set
                    StartingPlayerIndexLegs = (StartingPlayerIndexLegs + 1) % Players.Count;
                    CurrentPlayerIndex = StartingPlayerIndexLegs;

                    // Check best three dart leg average
                    if (currentPlayer.Stats.BestThreeDartLegAverage < currentPlayer.Stats.ThreeDartLegAverage)
                    {
                        currentPlayer.Stats.BestThreeDartLegAverage = currentPlayer.Stats.ThreeDartLegAverage;
                    }
                }

                if (PlayerScores[currentPlayer.Id].PlayerSets == NumOfSets)
                {
                    WinnerPopup = true;

                    await UpdateUserStats(currentPlayer);
                    await _StandardGamePersistence.Remove(GameCode);
                    return;
                }
            }
            else if (PlayerScores[currentPlayer.Id].PlayerScore < 0 || PlayerScores[currentPlayer.Id].PlayerScore == 1)
            {
                // Player has busted
                PlayerScores[currentPlayer.Id].PlayerScore += score;

                PlayerScores[currentPlayer.Id].PlayerThrows += 3;
                PlayerScores[currentPlayer.Id].PlayerThrowsLeg += 3;
                PlayerScores[currentPlayer.Id].PlayerCollectedScore += 0;

                currentPlayer.Stats.ThreeDartLegAverage = (double)((StartingScore - PlayerScores[currentPlayer.Id].PlayerScore) / ((double)PlayerScores[currentPlayer.Id].PlayerThrowsLeg / 3));
                currentPlayer.Stats.ThreeDartAverage = (double)(PlayerScores[currentPlayer.Id].PlayerCollectedScore / ((double)PlayerScores[currentPlayer.Id].PlayerThrows / 3));

                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            }
            else
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            }

            InputScore = "";
            InputScoreDartOne = "";
            InputScoreDartTwo = "";
            InputScoreDartThree = "";

            await SaveGameAsync();
        }

        private async Task UndoMove()
        {
            if (UndoStack.TryPop(out var snapshot))
            {
                CurrentPlayerIndex = snapshot.CurrentPlayerIndex;
                InputScore = snapshot.InputScore;

                // Restore player scores
                foreach (var kvp in snapshot.PlayerStates)
                {
                    if (PlayerScores.ContainsKey(kvp.Key))
                    {
                        PlayerScores[kvp.Key] = kvp.Value;
                    }
                }

                // Restore player stats
                foreach (var statSnapshot in snapshot.PlayerStatsSnapshot)
                {
                    var player = Players.FirstOrDefault(p => p.Id == statSnapshot.Id);
                    if (player != null)
                    {
                        player.Stats = statSnapshot.Stats;
                    }
                }
            }

            await SaveGameAsync();
        }

        private async Task UpdateUserStats(User user)
        {
            var existingUser = await _UserPersistence.GetUser(user.Id);
            if (existingUser == null)
            {
                return;
            }

            var current = existingUser.Stats;
            var recent = user.Stats;

            // Checkout percentage
            double totalHits = (current.CheckoutPercentage / 100.0) * current.NumOfDoublesThrown + (recent.CheckoutPercentage / 100.0) * recent.NumOfDoublesThrown;
            int totalAttempts = current.NumOfDoublesThrown + recent.NumOfDoublesThrown;
            current.CheckoutPercentage = (totalHits / totalAttempts) * 100;
            current.NumOfDoublesThrown = totalAttempts;

            // Three dart average
            double totalDartsThrown = current.TotalDartsThrown + recent.TotalDartsThrown;
            current.ThreeDartAverage = (current.ThreeDartAverage * current.TotalDartsThrown + recent.ThreeDartAverage * recent.TotalDartsThrown) / totalDartsThrown;
            current.TotalDartsThrown = totalDartsThrown;

            // High score hits
            foreach (var kvp in user.Stats.HighScoreHits)
            {
                if (current.HighScoreHits.ContainsKey(kvp.Key))
                    current.HighScoreHits[kvp.Key] += kvp.Value;
                else
                    current.HighScoreHits[kvp.Key] = kvp.Value;
            }

            // Darts per leg
            if (current.DartsPerLeg == 0)
                current.DartsPerLeg = recent.DartsPerLeg;
            else if (recent.DartsPerLeg == 0)
                current.DartsPerLeg = current.DartsPerLeg;
            else
                current.DartsPerLeg = (current.DartsPerLeg + recent.DartsPerLeg) / 2;

            // Best three dart leg average
            if (user.Stats.BestThreeDartLegAverage > current.BestThreeDartLegAverage)
            {
                current.BestThreeDartLegAverage = user.Stats.BestThreeDartLegAverage;
            }

            // Game history
            existingUser.GameHistory.Add(new OldGamesStats
            {
                GameCode = GameCode,
                GameStartingScore = StartingScore,
                OldThreeDartAverage = user.Stats.ThreeDartAverage,
                OldCheckoutPercentage = user.Stats.CheckoutPercentage,
                OldTotalDartsThrown = user.Stats.TotalDartsThrown,
                Timestamp = DateTime.UtcNow
            });

            await _UserPersistence.Update(existingUser);
        }
        private async Task SaveGameAsync()
        {
            var save = new StandardGame
            {
                Code = GameCode,
                LastModified = DateTime.UtcNow,
                Players = Players,
                StartingPoints = StartingScore,
                NumOfSets = NumOfSets,
                NumOfLegs = NumOfLegs,
                StartingIn = StartingIn,
                StartingOut = StartingOut,

                CurrentPlayerIndex = CurrentPlayerIndex,
                DartIndex = DartIndex,
                InputScoreDartOne = InputScoreDartOne,
                InputScoreDartTwo = InputScoreDartTwo,
                InputScoreDartThree = InputScoreDartThree,
                UseThreeDartMode = UseThreeDartMode,
                SelectedMultiplier = SelectedMultiplier,
                WinnerPopup = WinnerPopup,

                PlayerScores = PlayerScores,
                PlayerStats = Players.Select(p => new User
                {
                    Id = p.Id,
                    Name = p.Name,
                    Stats = p.Stats,
                    GameHistory = p.GameHistory
                }).ToList(),
                UndoHistory = UndoStack.Reverse().ToList()
            };

            await _StandardGamePersistence.AddOrUpdate(save);
        }

        private async Task ResetGame()
        {
            GameCode = Guid.NewGuid().ToString();

            CurrentPlayerIndex = 0;
            DartIndex = 1;

            InputScore = "";
            InputScoreDartOne = "";
            InputScoreDartTwo = "";
            InputScoreDartThree = "";

            WinnerPopup = false;
            UndoStack.Clear();

            foreach (var player in Players)
            {
                PlayerScores[player.Id] = new PlayerScoreInfo
                {
                    PlayerScore = StartingScore,
                    PlayerThrows = 0,
                    PlayerThrowsLeg = 0,
                    PlayerSets = 0,
                    PlayerLegs = 0,
                    PlayerCollectedScore = 0
                };

                player.Stats.HighestScore = 0;
                player.Stats.ThreeDartAverage = 0;
                player.Stats.ThreeDartLegAverage = 0;
                player.Stats.CheckoutPercentage = 0;
                player.Stats.NumOfDoublesThrown = 0;
                player.Stats.BestNumOfDartsThrown = 0;
                player.Stats.WorstNumOfDartsThrown = 0;
                player.Stats.BestThreeDartLegAverage = 0;
                player.Stats.DartsPerLeg = 0;
                player.Stats.TotalDartsThrown = 0;
                player.Stats.BestThreeDartLegAverage = 0;


                foreach (var key in player.Stats.HighScoreHits.Keys.ToList())
                {
                    player.Stats.HighScoreHits[key] = 0;
                }
            }

            await SaveGameAsync();
        }

        private void UpdateHighScoreHits(User currentPlayer, int roundScore)
        {
            if (roundScore == 180)
            {
                currentPlayer.Stats.HighScoreHits["180"]++;
            }
            else if (roundScore >= 160)
            {
                currentPlayer.Stats.HighScoreHits["160+"]++;
            }
            else if (roundScore >= 140)
            {
                currentPlayer.Stats.HighScoreHits["140+"]++;
            }
            else if (roundScore >= 120)
            {
                currentPlayer.Stats.HighScoreHits["120+"]++;
            }
            else if (roundScore >= 100)
            {
                currentPlayer.Stats.HighScoreHits["100+"]++;
            }
            else if (roundScore >= 80)
            {
                currentPlayer.Stats.HighScoreHits["80+"]++;
            }
            else if (roundScore >= 60)
            {
                currentPlayer.Stats.HighScoreHits["60+"]++;
            }
            else if (roundScore >= 40)
            {
                currentPlayer.Stats.HighScoreHits["40+"]++;
            }

            if (currentPlayer.Stats.HighestScore < roundScore)
            {
                currentPlayer.Stats.HighestScore = roundScore;
            }
        }


        private async Task HandleKey(KeyboardKey key)
        {
            if (key.Value == "DEL")
            {
                if (!string.IsNullOrEmpty(InputScore))
                {
                    InputScore = InputScore.Substring(0, InputScore.Length - 1);
                }
            }
            else if (key.Value == "UNDO")
            {
                await UndoMove();
                InputScore = "";
            }
            else
            {
                InputScore += key.Value;
            }
        }

        private async Task HandleKeyEachDart(KeyboardKey key)
        {
            if (key.Value == "DEL")
            {
                switch (DartIndex)
                {
                    case 1:
                        InputScoreDartOne = "";
                        break;

                    case 2:
                        if (!string.IsNullOrEmpty(InputScoreDartTwo))
                        {
                            InputScoreDartTwo = "";
                        }
                        else
                        {
                            DartIndex = 1;
                            InputScoreDartOne = "";
                        }
                        break;

                    case 3:
                        if (!string.IsNullOrEmpty(InputScoreDartThree))
                        {
                            InputScoreDartThree = "";
                        }
                        else
                        {
                            DartIndex = 2;
                            InputScoreDartTwo = "";
                        }
                        break;
                }
            }
            else if (key.Value == "UNDO")
            {
                await UndoMove();
                InputScoreDartOne = InputScoreDartTwo = InputScoreDartThree = "";
                DartIndex = 1;
            }
            else
            {
                if (key.Value == "MISS")
                {
                    await ApplyDart("0");
                }
                else
                {
                    await ApplyDart(key.Value);
                }
            }
        }
        private async Task ApplyDart(string value)
        {
            var currentPlayer = Players[CurrentPlayerIndex];

            switch (DartIndex)
            {
                case 1:
                    InputScoreDartOne = value;
                    if (GameSettings.EndInOption == "DOUBLE OUT" && SelectedMultiplier == "D" && PlayerScores[currentPlayer.Id].PlayerScore - int.Parse(value) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "MASTER OUT" && (SelectedMultiplier == "D" || SelectedMultiplier == "T") && PlayerScores[currentPlayer.Id].PlayerScore - int.Parse(value) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "STRAIGHT OUT" && PlayerScores[currentPlayer.Id].PlayerScore - int.Parse(value) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "DOUBLE OUT" && SelectedMultiplier != "D" && PlayerScores[currentPlayer.Id].PlayerScore - int.Parse(value) == 0)
                    {
                        InputScoreDartOne = "";
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (PlayerScores[currentPlayer.Id].PlayerScore - int.Parse(value) < 2)
                    {
                        // player bust
                        InputScoreDartOne = "";
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else
                    {
                        DartIndex = 2;
                    }
                    break;
                case 2:
                    InputScoreDartTwo = value;
                    if (GameSettings.EndInOption == "DOUBLE OUT" && SelectedMultiplier == "D" 
                        && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(value)) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "MASTER OUT" && (SelectedMultiplier == "D" || SelectedMultiplier == "T") 
                                && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(value)) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "STRAIGHT OUT" && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(value)) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if ((GameSettings.EndInOption == "DOUBLE OUT" && SelectedMultiplier != "D" 
                                && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(value)) == 0)
                                    || PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(value)) < 2)
                    {
                        InputScoreDartOne = "";
                        InputScoreDartTwo = "";
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else
                        DartIndex = 3;
                    break;
                case 3:
                    InputScoreDartThree = value;
                    if (GameSettings.EndInOption == "DOUBLE OUT" && SelectedMultiplier == "D"
                            && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(InputScoreDartTwo) + int.Parse(value)) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "MASTER OUT" && (SelectedMultiplier == "D" || SelectedMultiplier == "T") 
                                && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(InputScoreDartTwo) + int.Parse(value)) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if (GameSettings.EndInOption == "STRAIGHT OUT" && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(InputScoreDartTwo) + int.Parse(value)) == 0)
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else if ((GameSettings.EndInOption == "DOUBLE OUT" && SelectedMultiplier != "D" 
                                && PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(InputScoreDartTwo) + int.Parse(value)) == 0)
                                    || PlayerScores[currentPlayer.Id].PlayerScore - (int.Parse(InputScoreDartOne) + int.Parse(InputScoreDartTwo) + int.Parse(value)) < 2)
                    {
                        InputScoreDartOne = "";
                        InputScoreDartTwo = "";
                        InputScoreDartThree = "";
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    else
                    {
                        DartIndex = 1;
                        await SubmitScore();
                    }
                    break;
            }
        }
        private void GoBack()
        {
            NavManager.NavigateTo("/gamesStandard");
        }

        public KeyboardParameters KeyboardParams = new()
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new() { Text = "1", Value = "1" },
                    new() { Text = "2", Value = "2" },
                    new() { Text = "3", Value = "3" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "4", Value = "4" },
                    new() { Text = "5", Value = "5" },
                    new() { Text = "6", Value = "6" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "7", Value = "7" },
                    new() { Text = "8", Value = "8" },
                    new() { Text = "9", Value = "9" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "Undo", Value = "UNDO" },
                    new() { Text = "0", Value = "0" },
                    new() { Text = "Del", Value = "DEL" }  // Delete button
                }
            }
        };

        public KeyboardParameters KeyboardParamsEachDartSingle = new()
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new() { Text = "1", Value = "1" },
                    new() { Text = "2", Value = "2" },
                    new() { Text = "3", Value = "3" },
                    new() { Text = "4", Value = "4" },
                    new() { Text = "5", Value = "5" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "6", Value = "6" },
                    new() { Text = "7", Value = "7" },
                    new() { Text = "8", Value = "8" },
                    new() { Text = "9", Value = "9" },
                    new() { Text = "10", Value = "10" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "11", Value = "11" },
                    new() { Text = "12", Value = "12" },
                    new() { Text = "13", Value = "13" },
                    new() { Text = "14", Value = "14" },
                    new() { Text = "15", Value = "15" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "16", Value = "16" },
                    new() { Text = "17", Value = "17" },
                    new() { Text = "18", Value = "18" },
                    new() { Text = "19", Value = "19" },
                    new() { Text = "20", Value = "20" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "Undo", Value = "UNDO" },
                    new() { Text = "Bull(25)", Value = "25" },
                    new() { Text = "Miss", Value = "MISS" },
                    new() { Text = "Del", Value = "DEL" }
                }
            }
        };
        public KeyboardParameters KeyboardParamsEachDartDouble = new()
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new() { Text = "D1", Value = "2" },
                    new() { Text = "D2", Value = "4" },
                    new() { Text = "D3", Value = "6" },
                    new() { Text = "D4", Value = "8" },
                    new() { Text = "D5", Value = "10" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "D6", Value = "12" },
                    new() { Text = "D7", Value = "14" },
                    new() { Text = "D8", Value = "16" },
                    new() { Text = "D9", Value = "18" },
                    new() { Text = "D10", Value = "20" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "D11", Value = "22" },
                    new() { Text = "D12", Value = "24" },
                    new() { Text = "D13", Value = "26" },
                    new() { Text = "D14", Value = "28" },
                    new() { Text = "D15", Value = "30" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "D16", Value = "32" },
                    new() { Text = "D17", Value = "34" },
                    new() { Text = "D18", Value = "36" },
                    new() { Text = "D19", Value = "38" },
                    new() { Text = "D20", Value = "40" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "Undo", Value = "UNDO" },
                    new() { Text = "Bull(50)", Value = "50" },
                    new() { Text = "Miss", Value = "MISS" },
                    new() { Text = "Del", Value = "DEL" }
                }
            }
        };
        public KeyboardParameters KeyboardParamsEachDartTriple = new()
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new() { Text = "T1", Value = "3" },
                    new() { Text = "T2", Value = "6" },
                    new() { Text = "T3", Value = "9" },
                    new() { Text = "T4", Value = "12" },
                    new() { Text = "T5", Value = "15" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "T6", Value = "18" },
                    new() { Text = "T7", Value = "21" },
                    new() { Text = "T8", Value = "24" },
                    new() { Text = "T9", Value = "27" },
                    new() { Text = "T10", Value = "30" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "T11", Value = "33" },
                    new() { Text = "T12", Value = "36" },
                    new() { Text = "T13", Value = "39" },
                    new() { Text = "T14", Value = "42" },
                    new() { Text = "T15", Value = "45" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "T16", Value = "48" },
                    new() { Text = "T17", Value = "51" },
                    new() { Text = "T18", Value = "54" },
                    new() { Text = "T19", Value = "57" },
                    new() { Text = "T20", Value = "60" }
                },
                new List<KeyboardKey>
                {
                    new() { Text = "Undo", Value = "UNDO" },
                    new() { Text = "Miss", Value = "MISS" },
                    new() { Text = "Del", Value = "DEL" }
                }
            }
        };



        // Creating suggestion dictionary
        public Dictionary<string, List<string>> SuggestionFinishes = new Dictionary<string, List<string>>()
        {
            { "170", new List<string> { "T20", "T20", "DB" } },
            { "167", new List<string> { "T20", "T19", "DB" } },
            { "164", new List<string> { "T19", "T19", "DB" } },
            { "161", new List<string> { "T20", "T17", "DB" } },
            { "160", new List<string> { "T20", "T20", "D20" } },
            { "158", new List<string> { "T20", "T20", "D19" } },
            { "157", new List<string> { "T20", "T19", "D20" } },
            { "156", new List<string> { "T20", "T20", "D18" } },
            { "155", new List<string> { "T20", "T19", "D19" } },
            { "154", new List<string> { "T20", "T18", "D20" } },
            { "153", new List<string> { "T20", "T19", "D18" } },
            { "152", new List<string> { "T20", "T20", "D16" } },
            { "151", new List<string> { "T20", "T17", "D20" } },
            { "150", new List<string> { "T20", "T18", "D18" } },
            { "149", new List<string> { "T20", "T19", "D16" } },
            { "148", new List<string> { "T20", "T16", "D20" } },
            { "147", new List<string> { "T20", "T17", "D18" } },
            { "146", new List<string> { "T20", "T18", "D16" } },
            { "145", new List<string> { "T20", "T15", "D20" } },
            { "144", new List<string> { "T20", "T20", "D12" } },
            { "143", new List<string> { "T20", "T17", "D16" } },
            { "142", new List<string> { "T20", "T14", "D20" } },
            { "141", new List<string> { "T20", "T19", "D12" } },
            { "140", new List<string> { "T20", "T20", "D10" } },
            { "139", new List<string> { "T19", "T14", "D20" } },
            { "138", new List<string> { "T20", "T18", "D12" } },
            { "137", new List<string> { "T20", "T19", "D10" } },
            { "136", new List<string> { "T20", "T20", "D8" } },
            { "135", new List<string> { "T20", "T17", "D12" } },
            { "134", new List<string> { "T20", "T14", "D16" } },
            { "133", new List<string> { "T20", "T19", "D8" } },
            { "132", new List<string> { "T20", "T16", "D12" } },
            { "131", new List<string> { "T20", "T13", "D16" } },
            { "130", new List<string> { "T20", "T18", "D8" } },
            { "129", new List<string> { "T19", "T16", "D12" } },
            { "128", new List<string> { "T18", "T14", "D16" } },
            { "127", new List<string> { "T20", "T17", "D8" } },
            { "126", new List<string> { "T19", "T19", "D6" } },
            { "125", new List<string> { "25", "T20", "D20" } },
            { "124", new List<string> { "T20", "T16", "D8" } },
            { "123", new List<string> { "T19", "T16", "D9" } },
            { "122", new List<string> { "T18", "T18", "D7" } },
            { "121", new List<string> { "T20", "T11", "D14" } },
            { "120", new List<string> { "20", "T20", "D20" } },
            { "119", new List<string> { "T19", "T12", "D13" } },
            { "118", new List<string> { "20", "18", "D20" } },
            { "117", new List<string> { "20", "17", "D20" } },
            { "116", new List<string> { "20", "16", "D20" } },
            { "115", new List<string> { "19", "18", "D20" } },
            { "114", new List<string> { "20", "14", "D20" } },
            { "113", new List<string> { "20", "13", "D20" } },
            { "112", new List<string> { "T20", "12", "D20" } },
            { "111", new List<string> { "T20", "19", "D16" } },
            { "110", new List<string> { "T20", "10", "D20" } },
            { "109", new List<string> { "T20", "9", "D20" } },
            { "108", new List<string> { "T20", "16", "D20" } },
            { "107", new List<string> { "T19", "10", "D20" } },
            { "106", new List<string> { "T20", "6", "D20" } },
            { "105", new List<string> { "T19", "6", "D20" } },
            { "104", new List<string> { "T18", "10", "D20" } },
            { "103", new List<string> { "T20", "11", "D16" } },
            { "102", new List<string> { "T20", "10", "D16" } },
            { "101", new List<string> { "T17", "10", "D20" } },
            { "100", new List<string> { "T20", "D20" } },
            { "99", new List<string> { "T19", "10", "D16" } },
            { "98", new List<string> { "T20", "18", "D10" } },
            { "97", new List<string> { "T19", "20", "D10" } },
            { "96", new List<string> { "T20", "D18" } },
            { "95", new List<string> { "T19", "D19" } },
            { "94", new List<string> { "T18", "D20" } },
            { "93", new List<string> { "T19", "D18" } },
            { "92", new List<string> { "T20", "D16" } },
            { "91", new List<string> { "T17", "D20" } },
            { "90", new List<string> { "T20", "D15" } },
            { "89", new List<string> { "T19", "D16" } },
            { "88", new List<string> { "T16", "D20" } },
            { "87", new List<string> { "T17", "D18" } },
            { "86", new List<string> { "T18", "D16" } },
            { "85", new List<string> { "T15", "D20" } },
            { "84", new List<string> { "T20", "D12" } },
            { "83", new List<string> { "T17", "D16" } },
            { "82", new List<string> { "Bull", "D16" } },
            { "81", new List<string> { "T19", "D12" } },
            { "80", new List<string> { "T20", "D10" } },
            { "79", new List<string> { "T13", "D20" } },
            { "78", new List<string> { "T18", "D12" } },
            { "77", new List<string> { "T19", "D10" } },
            { "76", new List<string> { "T20", "D8" } },
            { "75", new List<string> { "T17", "D12" } },
            { "74", new List<string> { "T14", "D16" } },
            { "73", new List<string> { "T19", "D8" } },
            { "72", new List<string> { "T16", "D12" } },
            { "71", new List<string> { "T13", "D16" } },
            { "70", new List<string> { "T18", "D8" } },
            { "69", new List<string> { "T19", "D6" } },
            { "68", new List<string> { "T20", "D4" } },
            { "67", new List<string> { "T17", "D8" } },
            { "66", new List<string> { "T10", "D18" } },
            { "65", new List<string> { "SB", "D20" } },
            { "64", new List<string> { "T16", "D8" } },
            { "63", new List<string> { "T17", "D6" } },
            { "62", new List<string> { "T10", "D16" } },
            { "61", new List<string> { "SB", "D18" } },
            { "60", new List<string> { "20", "D20" } },
            { "59", new List<string> { "19", "D20" } },
            { "58", new List<string> { "18", "D20" } },
            { "57", new List<string> { "17", "D20" } },
            { "56", new List<string> { "16", "D20" } },
            { "55", new List<string> { "15", "D20" } },
            { "54", new List<string> { "14", "D20" } },
            { "53", new List<string> { "13", "D20" } },
            { "52", new List<string> { "12", "D20" } },
            { "51", new List<string> { "11", "D20" } },
            { "50", new List<string> { "10", "D20" } },
            { "49", new List<string> { "9", "D20" } },
            { "48", new List<string> { "16", "D16" } },
            { "47", new List<string> { "15", "D16" } },
            { "46", new List<string> { "14", "D16" } },
            { "45", new List<string> { "13", "D16" } },
            { "44", new List<string> { "12", "D16" } },
            { "43", new List<string> { "11", "D16" } },
            { "42", new List<string> { "10", "D16" } },
            { "41", new List<string> { "9", "D16" } },
            { "40", new List<string> { "D20" } },
            { "39", new List<string> { "7", "D16" } },
            { "38", new List<string> { "D19" } },
            { "37", new List<string> { "5","D16" } },
            { "36", new List<string> { "D18" } },
            { "35", new List<string> { "3", "D16" } },
            { "34", new List<string> { "D17" } },
            { "33", new List<string> { "1", "D16" } },
            { "32", new List<string> { "D16" } },
            { "31", new List<string> { "15", "D8" } },
            { "30", new List<string> { "D15" } },
            { "29", new List<string> { "13", "D8" } },
            { "28", new List<string> { "D14" } },
            { "27", new List<string> { "11", "D8" } },
            { "26", new List<string> { "D13" } },
            { "25", new List<string> { "9", "D8" } },
            { "24", new List<string> { "D12" } },
            { "23", new List<string> { "7", "D8" } },
            { "22", new List<string> { "D11" } },
            { "21", new List<string> { "5", "D8" } },
            { "20", new List<string> { "D10" } },
            { "19", new List<string> { "3", "D8" } },
            { "18", new List<string> { "D9" } },
            { "17", new List<string> { "1", "D8" } },
            { "16", new List<string> { "D8" } },
            { "15", new List<string> { "7", "D4" } },
            { "14", new List<string> { "D7" } },
            { "13", new List<string> { "5", "D4" } },
            { "12", new List<string> { "D6" } },
            { "11", new List<string> { "3", "D4" } },
            { "10", new List<string> { "D5" } },
            { "9", new List<string> { "1", "D4" } },
            { "8", new List<string> { "D4" } },
            { "7", new List<string> { "3", "D2" } },
            { "6", new List<string> { "D3" } },
            { "5", new List<string> { "1", "D2" } },
            { "4", new List<string> { "D2" } },
            { "3", new List<string> { "1", "D1" } },
            { "2", new List<string> { "D1" } }

        };
    }
}
