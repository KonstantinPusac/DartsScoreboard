
using Microsoft.AspNetCore.Components;

namespace DartsScoreboard;

public partial class CricketGamePage
{
    [Inject] public ICricketPersistence _CricketPersistence { get; set; }
    [Inject] public IUserPersistence _UserPersistence { get; set; }
    [Inject] public NavigationManager _NavigationManager { get; set; }
    [Parameter] public string? gameCode { get; set; }
    public int Round { get; set; } = 1;
    public bool Loaded { get; set; }
    public bool IsGameFinished { get; set; }
    public CricketGame Game { get; set; } = new();
    public List<CricketPlayerPresenter> Players { get; set; } = new();
    public KeyboardParameters KeyboardParameters { get; set; }
    public int PlayerOnTurnIndex { get; set; } = 0; // index of player on turn in Players list
    public CricketPlayerPresenter PlayerOnTurn => Players[PlayerOnTurnIndex]; // player on turn in current round

    public CricketGameThrowPresenter CurrentThrow => PlayerOnTurn.CurrentThrow;// in current round - max 9 elements  
    private readonly Stack<CricketGameStateSnapshot> _undoStack = new();
    private readonly Stack<CricketGameStateSnapshot> _redoStack = new();
    private bool _canUndo => _undoStack.Count > 0;
    private bool _canRedo => _redoStack.Count > 0;
    private List<User> _usersCache = new();
    protected override void OnInitialized()
    {
        KeyboardParameters = new KeyboardParameters
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "20", Value = "20",IsDisabled=()=>IsDisabledKey("20"),HitCount=()=>GetHitCount("20"),Points=()=>GetPoints("20"),Style=()=>GetStyle("20") },
                    new KeyboardKey { Text = "19", Value = "19" ,IsDisabled=()=>IsDisabledKey("19"),HitCount=()=>GetHitCount("19"),Points=()=>GetPoints("19"),Style=()=>GetStyle("19") },
                    new KeyboardKey { Text = "18", Value = "18",IsDisabled=()=>IsDisabledKey("18"),HitCount=()=>GetHitCount("18") ,Points=()=>GetPoints("18"),Style=()=>GetStyle("18") },
                },
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "17", Value = "17",IsDisabled=()=>IsDisabledKey("17") ,HitCount=()=>GetHitCount("17"),Points=()=>GetPoints("17"),Style=()=>GetStyle("17") },
                    new KeyboardKey { Text = "16", Value = "16" ,IsDisabled=()=>IsDisabledKey("16"), HitCount =() => GetHitCount("16"), Points =() => GetPoints("16"),Style=()=>GetStyle("16") },
                    new KeyboardKey { Text = "15", Value = "15",IsDisabled=()=>IsDisabledKey("15") , HitCount =() => GetHitCount("15"), Points =() => GetPoints("15"),Style=()=>GetStyle("15") },
                },
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "⌫", Value = "BACKSPACE" },
                    new KeyboardKey { Text = "BULL", Value = "BULL",IsDisabled=()=>IsDisabledKey("BULL") , HitCount =() => GetHitCount("BULL"), Points =() => GetPoints("BULL"),Style=()=>GetStyle("BULL") },
                    new KeyboardKey { Text = "ENTER", Value = "ENTER" },
                },
            }
        };
    }

    private string GetStyle(string key)
    {
        if (IsDisabledKey(key))
            return "";
        if (PlayerOnTurn.Scores.GetValueOrDefault(key, 0) + CurrentThrow.Score.Count(x => x.Target == key) >= 3)
            return "color:#78fa78;";
        if (Players.Where(x => x != PlayerOnTurn).Any(x => x.Scores.GetValueOrDefault(key, 0) >= 3))
            return "color:#fa7878;";
        return "";
    }

    private bool IsDisabledKey(string key)
    {
        return PlayerOnTurn.Scores.GetValueOrDefault(key, 0) + CurrentThrow.Score.Count(x => x.Target == key) == 3
               && !Players.Where(x => x != PlayerOnTurn).Any(x => x.Scores.GetValueOrDefault(key, 0) < 3);
    }
    private int GetHitCount(string key)
    {
        return CurrentThrow.Score.Count(x => x.Target == key);
    }
    public int GetTableScore(CricketPlayerPresenter player, KeyValuePair<string, int> score)
    {
        return score.Value + player.CurrentThrow.Score.Count(x => x.Target == score.Key);
    }
    private int GetPoints(string key)
    {
        return GetTargetValue(key) * CurrentThrow.Score.Count(x => x.Target == key && x.ArePoints);
    }
    private int CalculatePoints(CricketPlayerPresenter player)
    {
        if (player == PlayerOnTurn)
            return player.Points;
        return player.Points + CurrentThrow.Score.Where(x => x.ArePoints && player.Scores.GetValueOrDefault(x.Target, 0) < 3).Sum(x => GetTargetValue(x.Target));
    }
    protected override async Task OnParametersSetAsync()
    {
        if (gameCode == null)
        {
            return;
        }
        var game = await _CricketPersistence.Get(gameCode);
        if (game == null)
        {
            _NavigationManager.NavigateTo($"/cricket-setup");
            return;
        }
        Game = game;
        var users = await _UserPersistence.GetAllUsers();
        _usersCache = users;
        Players = Game.Players.Select(x => new CricketPlayerPresenter
        {
            Throws = x.Throws,
            Points = x.Points,
            Scores = x.Scores.ToDictionary(x => x.Target, x => x.Count),
            UserId = x.UserId,
            Name = users.FirstOrDefault(u => u.Id == x.UserId)?.Name ?? x.Name ?? "Guest",
        }).ToList();

        ResolvePlayerOnTurn();
        UpdateCurrentPlayerDeficit();
        UpdateCurrentTurnStats();
        Loaded = true;
    }

    public async Task KeyboardClick(KeyboardKey keyboardKey)
    {
        bool needsUpdate = false;
        Console.WriteLine(keyboardKey.Value);
        if (keyboardKey.Value == "BACKSPACE")
        {
            if (CurrentThrow.Score.Count == 0)
                return;
            var lastScore = CurrentThrow.Score.Last();
            var lastTargetValue = GetTargetValue(lastScore.Target);
            CurrentThrow.Score.Remove(lastScore);
            var players = CurrentThrow.PointsForPlayers.Where(x => x.Points.LastOrDefault() == lastTargetValue).ToList();
            foreach (var player in players)
            {
                player.Points.RemoveAt(player.Points.Count - 1);
            }
            needsUpdate = true;
        }
        else if (keyboardKey.Value == "ENTER")
        {
            await AddThrow(CurrentThrow, false);
            return;
        }
        else if (!IsDisabledKey(keyboardKey.Value) && CanAddScore(keyboardKey.Value))
        {
            int marks = GetPlayerOnTurnMarks(keyboardKey.Value);
            if (marks < 3)
            {
                CurrentThrow.Score.Add(new CricketThrowScore
                {
                    Target = keyboardKey.Value,
                    ArePoints = false,
                });
                needsUpdate = true;
            }
            else
            {
                CurrentThrow.Score.Add(new CricketThrowScore
                {
                    Target = keyboardKey.Value,
                    ArePoints = true,
                });
                var playersToAddPoints = Players.Where(x => x != PlayerOnTurn && x.Scores.GetValueOrDefault(keyboardKey.Value, 0) < 3).ToList();
                foreach (var item in playersToAddPoints)
                {
                    var existing = CurrentThrow.PointsForPlayers.FirstOrDefault(x => x.Player == item);
                    if (existing != null)
                        existing.Points.Add(GetTargetValue(keyboardKey.Value));
                    else
                        CurrentThrow.PointsForPlayers.Add(new PointsForPlayer
                        {
                            Player = item,
                            Points = new List<int> { GetTargetValue(keyboardKey.Value) },
                        });
                }
                needsUpdate = true;
            }
        }

        if (needsUpdate)
        {
            UpdateCurrentPlayerDeficit();
            UpdateCurrentTurnStats();
            StateHasChanged();
        }
    }
    private string GetClass(CricketPlayerPresenter playerPresenter)
    {
        return PlayerOnTurn != playerPresenter ? "player-row" : "player-row player-row-selected";
    }
    private int GetPlayerOnTurnMarks(string key)
    {
        int marks = PlayerOnTurn.Scores.GetValueOrDefault(key, 0) + CurrentThrow.Score.Count(x => x.Target == key);
        return marks > 3 ? 3 : marks;
    }
    private bool CanAddScore(string key)
    {
        if (CurrentThrow.Score.Count == 9)
            return false;
        var targetGroups = CurrentThrow.Score.GroupBy(x => x.Target).ToDictionary(x => x.Key, x => x.Count());

        if (targetGroups.ContainsKey(key))
            targetGroups[key]++;
        else
            targetGroups.Add(key, 1);


        int totalThrows = targetGroups.Count
                        + targetGroups.Count(x => x.Value > 3 || (x.Value > 2 && x.Key == "BULL"))
                        + targetGroups.Count(x => x.Value > 6 || (x.Value > 4 && x.Key == "BULL"));

        return totalThrows <= 3;
    }

    private async Task AddThrow(CricketGameThrowPresenter cricketThrow, bool isRedo)
    {
        if (!isRedo)
        {
            _undoStack.Push(CreateSnapshot());
            _redoStack.Clear();
        }
        var scoreGroups = cricketThrow.Score.GroupBy(x => x.Target);
        PlayerOnTurn.Throws.Add(new CricketThrow()
        {
            Score = scoreGroups.Select(x => new CricketNumberScore
            {
                Count = x.Count(),
                Target = x.Key,
            }).ToList()
        });
        foreach (var item in scoreGroups)
        {
            if (PlayerOnTurn.Scores.ContainsKey(item.Key))
                PlayerOnTurn.Scores[item.Key] += item.Count();
            else
                PlayerOnTurn.Scores[item.Key] = item.Count();
        }
        foreach (var player in cricketThrow.PointsForPlayers)
        {
            player.Player.Points += player.Points.Sum();
        }

        if (IsEndOfGame())
        {
            IsGameFinished = true;
            await EndOfGame();
            return;
        }
        CurrentThrow.Clear();
        if (Players.Count == (PlayerOnTurnIndex + 1))
        {
            PlayerOnTurnIndex = 0;
            Round++;
        }
        else
        {
            PlayerOnTurnIndex++;
        }
        //AddToStack(cricketThrow, false);
        await Save();
        UpdateCurrentPlayerDeficit();
        UpdateCurrentTurnStats();
    }

    private CricketGameStateSnapshot CreateSnapshot()
    {
        // Build a deep copy of the current state using the same mapping as Save()
        var playersCopy = Players.Select(x => new CricketPlayer
        {
            UserId = x.UserId,
            Name = x.Name,
            Throws = x.Throws.Select(t => new CricketThrow
            {
                Score = t.Score.Select(s => new CricketNumberScore
                {
                    Target = s.Target,
                    Count = s.Count,
                }).ToList(),
            }).ToList(),
            Scores = x.Scores.Select(s => new CricketNumberScore
            {
                Target = s.Key,
                Count = s.Value,
            }).ToList(),
            Points = x.Points,
        }).ToList();

        return new CricketGameStateSnapshot
        {
            Players = playersCopy,
            PlayerOnTurnIndex = PlayerOnTurnIndex,
            Round = Round,
            IsGameFinished = IsGameFinished,
        };
    }

    private void RestoreSnapshot(CricketGameStateSnapshot snap)
    {
        // Rebuild presenters from snapshot
        Players = snap.Players.Select(x => new CricketPlayerPresenter
        {
            Throws = x.Throws.Select(t => new CricketThrow
            {
                Score = t.Score.Select(s => new CricketNumberScore
                {
                    Target = s.Target,
                    Count = s.Count,
                }).ToList(),
            }).ToList(),
            Scores = x.Scores.ToDictionary(s => s.Target, s => s.Count),
            Points = x.Points,
            UserId = x.UserId,
            Name = _usersCache.FirstOrDefault(u => u.Id == x.UserId)?.Name ?? x.Name ?? "Guest",
        }).ToList();
        PlayerOnTurnIndex = Math.Clamp(snap.PlayerOnTurnIndex, 0, Math.Max(Players.Count - 1, 0));
        Round = Math.Max(1, snap.Round);
        IsGameFinished = snap.IsGameFinished;
        if (Players.Count > 0)
            Players[PlayerOnTurnIndex].CurrentThrow.Clear();
    }

    public async Task Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(CreateSnapshot());
        var snap = _undoStack.Pop();
        RestoreSnapshot(snap);
        await Save();
        UpdateCurrentPlayerDeficit();
        UpdateCurrentTurnStats();
        StateHasChanged();
    }

    public async Task Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(CreateSnapshot());
        var snap = _redoStack.Pop();
        RestoreSnapshot(snap);
        await Save();
        UpdateCurrentPlayerDeficit();
        UpdateCurrentTurnStats();
        StateHasChanged();
    }

    private class CricketGameStateSnapshot
    {
        public List<CricketPlayer> Players { get; set; } = new();
        public int PlayerOnTurnIndex { get; set; }
        public int Round { get; set; }
        public bool IsGameFinished { get; set; }
    }
    private async Task Save()
    {
        Game.Players = Players.Select(x => new CricketPlayer
        {
            UserId = x.UserId,
            Name = x.Name,
            Throws = x.Throws.Select(t => new CricketThrow
            {
                Score = t.Score.Select(s => new CricketNumberScore
                {
                    Target = s.Target,
                    Count = s.Count,
                }).ToList(),
            }).ToList(),
            Scores = x.Scores.Select(s => new CricketNumberScore
            {
                Target = s.Key,
                Count = s.Value,
            }).ToList(),
            Points = x.Points,
        }).ToList();
        Game.IsGameFinished = IsGameFinished;
        await _CricketPersistence.AddOrUpdate(Game);
    }

    private void ResolvePlayerOnTurn()
    {
        int minThrowCount = Players.Min(x => x.Throws.Count);
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].Throws.Count == minThrowCount)
            {
                PlayerOnTurnIndex = i;
                return;
            }
        }
        PlayerOnTurnIndex = 0;
    }

    private async Task EndOfGame()
    {
        // Save final game state
        await Save();

        // Navigate to end game page
        _NavigationManager.NavigateTo($"/cricket-end/{gameCode}");
    }

    private bool IsEndOfGame()
    {
        // Check if any player has all 21 marks (3 marks for each target: 20,19,18,17,16,15,BULL)
        var targets = new[] { "20", "19", "18", "17", "16", "15", "BULL" };

        foreach (var player in Players)
        {
            bool hasAllMarks = targets.All(target =>
                player.Scores.GetValueOrDefault(target, 0) >= 3);

            if (hasAllMarks)
            {
                // Check if this player has minimal points (wins) or if there's a tie
                int playerPoints = CalculatePoints(player);
                int minPoints = Players.Min(p => CalculatePoints(p));

                // Game ends if this player has minimal points (wins) 
                // OR if multiple players have 21 marks and same minimal points (tie)
                if (playerPoints == minPoints)
                {
                    return true;
                }
            }
        }

        return false;
    }
    int GetTargetValue(string key)
    {
        if (key.Equals("BULL", StringComparison.InvariantCultureIgnoreCase))
            return 25;
        return int.TryParse(key, out int value) ? value : 0;
    }

    private void UpdateCurrentPlayerDeficit()
    {
        if (Players == null || Players.Count < 2)
        {
            CurrentPlayerDeficit = 0;
            return;
        }

        int currentPoints = CalculatePoints(PlayerOnTurn);
        int minPoints = int.MaxValue;

        foreach (var player in Players)
        {
            if (player != PlayerOnTurn)
            {
                int playerPoints = CalculatePoints(player);
                if (playerPoints < minPoints)
                    minPoints = playerPoints;
            }
        }

        CurrentPlayerDeficit = currentPoints - minPoints;
    }

    private void UpdateCurrentTurnStats()
    {
        // Number of entries (marks + points) recorded in this turn
        CurrentPlayerMarks = CurrentThrow.Score.Count;
        // Sum of points that will be awarded this turn (after closing)
        CurrentThrowPoints = CurrentThrow.PointsForPlayers.Sum(p => p.Points.Sum());
    }
}
