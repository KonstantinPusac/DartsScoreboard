
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;

namespace DartsScoreboard;

public partial class CricketGamePage
{
    [Inject] public ICricketPersistence _CricketPersistence { get; set; }
    [Inject] public IUserPersistence _UserPersistence { get; set; }
    [Inject] public NavigationManager _NavigationManager { get; set; }
    [Parameter] public string? gameCode { get; set; }
    public int Round { get; set; } = 1;
    public CricketGame Game { get; set; } = new();
    public List<CricketPlayerPresenter> Players { get; set; } = new();
    public KeyboardParameters KeyboardParameters { get; set; }
    public CricketPlayerPresenter PlayerOnTurn { get; set; }

    public int CurrentAllScoreIntex { get; set; } = 0;
    public List<CricketGameThrowPresenter> AllScores { get; set; }
    public List<CricketThrowScore> CurrentThrowScore { get; set; } = new();// in current round - max 9 elements
    public List<PointsForPlayer> CurrentThrowPointsByPlayer { get; set; } = new();//in current round
    protected override void OnInitialized()
    {
        KeyboardParameters = new KeyboardParameters
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "20", Value = "20",IsDisabled=()=>IsDisabledKey("20"),HitCount=()=>GetHitCount("20"),Points=()=>GetPoints("20") },
                    new KeyboardKey { Text = "19", Value = "19" ,IsDisabled=()=>IsDisabledKey("19"),HitCount=()=>GetHitCount("19"),Points=()=>GetPoints("19")},
                    new KeyboardKey { Text = "18", Value = "18",IsDisabled=()=>IsDisabledKey("18"),HitCount=()=>GetHitCount("18") ,Points=()=>GetPoints("18")},
                },
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "17", Value = "17",IsDisabled=()=>IsDisabledKey("17") ,HitCount=()=>GetHitCount("17"),Points=()=>GetPoints("17")},
                    new KeyboardKey { Text = "16", Value = "16" ,IsDisabled=()=>IsDisabledKey("16"), HitCount =() => GetHitCount("16"), Points =() => GetPoints("16")},
                    new KeyboardKey { Text = "15", Value = "15",IsDisabled=()=>IsDisabledKey("15") , HitCount =() => GetHitCount("15"), Points =() => GetPoints("15")},
                },
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "⌫", Value = "BACKSPACE" },
                    new KeyboardKey { Text = "BULL", Value = "BULL",IsDisabled=()=>IsDisabledKey("BULL") , HitCount =() => GetHitCount("BULL"), Points =() => GetPoints("BULL")},
                    new KeyboardKey { Text = "↵", Value = "ENTER" },
                },
            }
        };
    }
    private bool IsDisabledKey(string key)
    {
        return PlayerOnTurn.Scores.GetValueOrDefault(key, 0) == 3
               && !Players.Where(x => x != PlayerOnTurn).Any(x => x.Scores.GetValueOrDefault(key, 0) < 3);
    }
    private int GetHitCount(string key)
    {
        return CurrentThrowScore.Count(x => x.Target == key);
    }
    private int GetPoints(string key)
    {
        return GetTargetValue(key) * CurrentThrowScore.Count(x => x.Target == key && x.ArePoints);
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
        if (IsEndOfGame())
        {
            await EndOfGame();
            return;
        }
        var users = await _UserPersistence.GetAllUsers();
        Players = Game.Players.Select(x => new CricketPlayerPresenter
        {
            Throws = x.Throws,
            Points = x.Points,
            Scores = x.Scores.ToDictionary(x => x.Target, x => x.Count),
            UserId = x.UserId,
            Name = users.FirstOrDefault(u => u.Id == x.UserId)?.Name ?? x.GuestName ?? "Guest",
        }).ToList();

        ResolvePlayerOnTurn();
    }

    public async Task KeyboardClick(KeyboardKey keyboardKey)
    {

        if (keyboardKey.Value == "BACKSPACE")
        {
            if (CurrentThrowScore.Count == 0)
                return;
            var lastScore = CurrentThrowScore.Last();
            var lastTargetValue = GetTargetValue(lastScore.Target);
            CurrentThrowScore.Remove(lastScore);
            var players = CurrentThrowPointsByPlayer.Where(x => x.Points.LastOrDefault() == lastTargetValue).ToList();
            foreach (var player in players)
            {
                player.Points.RemoveAt(player.Points.Count - 1);
            }
            return;
        }
        if (keyboardKey.Value == "ENTER")
        {
            await SaveThrow();
        }
        if (IsDisabledKey(keyboardKey.Value))
        {
            return;
        }
        if (!CanAddScore(keyboardKey.Value))
        {
            return;
        }

        int marks = GetPlayerOnTurnMarks(keyboardKey.Value);
        if (marks < 3)
        {
            CurrentThrowScore.Add(new CricketThrowScore
            {
                Target = keyboardKey.Value,
                ArePoints = false,
            });
        }
        CurrentThrowScore.Add(new CricketThrowScore
        {
            Target = keyboardKey.Value,
            ArePoints = true,
        });
        var playersToAddPoints = Players.Where(x => x != PlayerOnTurn && x.Scores.GetValueOrDefault(keyboardKey.Value, 0) < 3).ToList();
        foreach (var item in playersToAddPoints)
        {
            var existing = CurrentThrowPointsByPlayer.FirstOrDefault(x => x.Player == item);
            if (existing != null)
                existing.Points.Add(GetTargetValue(keyboardKey.Value));
            else
                CurrentThrowPointsByPlayer.Add(new PointsForPlayer
                {
                    Player = item,
                    Points = new List<int> { GetTargetValue(keyboardKey.Value) },
                });
        }
    }
    private int GetPlayerOnTurnMarks(string key)
    {
        int marks = PlayerOnTurn.Scores.GetValueOrDefault(key, 0) + CurrentThrowScore.Count(x => x.Target == key);
        return marks > 3 ? 3 : marks;
    }
    private bool CanAddScore(string key)
    {
        if (CurrentThrowScore.Count == 9)
            return false;
        var targetGroups = CurrentThrowScore.GroupBy(x => x.Target).ToDictionary(x => x.Key, x => x.Count());

        if (targetGroups.ContainsKey(key))
            targetGroups[key]++;
        else
            targetGroups.Add(key, 1);


        int totalThrows = targetGroups.Count
                        + targetGroups.Count(x => x.Value > 3 || (x.Value > 2 && x.Key == "BULL"))
                        + targetGroups.Count(x => x.Value > 6 || (x.Value > 4 && x.Key == "BULL"));

        return totalThrows <= 3;
    }

    private async Task SaveThrow()
    {
        var scoreGroups = CurrentThrowScore.GroupBy(x => x.Target);
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
        foreach (var player in CurrentThrowPointsByPlayer)
        {
            player.Player.Points += player.Points.Sum();
        }
    }

    private void ResolvePlayerOnTurn()
    {
        return;
        throw new NotImplementedException();
    }

    private async Task EndOfGame()
    {
        return;
        throw new NotImplementedException();
    }

    private bool IsEndOfGame()
    {
        return false;
        throw new NotImplementedException();
    }
    int GetTargetValue(string key)
    {
        if (key.Equals("BULL", StringComparison.InvariantCultureIgnoreCase))
            return 25;
        return int.TryParse(key, out int value) ? value : 0;
    }
}
