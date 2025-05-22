
using Microsoft.AspNetCore.Components;

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
    public List<CricketNumberScore> CurrentThrowScore { get; set; } = new();//3 elements for 3 darts


    protected override void OnInitialized()
    {
        KeyboardParameters = new KeyboardParameters
        {
            KeyboardKeys = new List<List<KeyboardKey>>
            {
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "20", Value = "20",IsDisabled=()=>IsDisabledKey("20"),HitCount=()=>GetHitCount("20") },
                    new KeyboardKey { Text = "19", Value = "19" ,IsDisabled=()=>IsDisabledKey("19"),HitCount=()=>GetHitCount("19")},
                    new KeyboardKey { Text = "18", Value = "18",IsDisabled=()=>IsDisabledKey("18"),HitCount=()=>GetHitCount("18") },
                },
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "17", Value = "17",IsDisabled=()=>IsDisabledKey("17") ,HitCount=()=>GetHitCount("17")},
                    new KeyboardKey { Text = "16", Value = "16" ,IsDisabled=()=>IsDisabledKey("16"), HitCount =() => GetHitCount("16")},
                    new KeyboardKey { Text = "15", Value = "15",IsDisabled=()=>IsDisabledKey("15") , HitCount =() => GetHitCount("15")},
                },
                new List<KeyboardKey>
                {
                    new KeyboardKey { Text = "⌫", Value = "BACKSPACE" },
                    new KeyboardKey { Text = "BULL", Value = "BULL",IsDisabled=()=>IsDisabledKey("BULL") , HitCount =() => GetHitCount("BULL")},
                    new KeyboardKey { Text = "↵", Value = "ENTER" },
                },
            }
        };
    }
    private bool IsDisabledKey(string key)
    {
        return Players.Where(x => x != PlayerOnTurn).Any(x => (x.Scores.FirstOrDefault(s => s.Target == key)?.Count ?? 0) < 3);
    }
    private int GetHitCount(string key)
    {
        return CurrentThrowScore.Where(x => x.Target == key).Sum(x => x.Count);
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
            Scores = x.Scores,
            UserId = x.UserId,
            Name = users.FirstOrDefault(u => u.Id == x.UserId)?.Name ?? x.GuestName ?? "Guest",
        }).ToList();

        ResolvePlayerOnTurn();
    }

    public async Task KeyboardClick(KeyboardKey keyboardKey)
    {

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
}
