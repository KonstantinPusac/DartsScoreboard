using Microsoft.AspNetCore.Components;

namespace DartsScoreboard;

public partial class CricketPracticeFinalPage
{
    [Inject] public ICricketPracticeGamePersistence _CricketPracticeGamePersistence { get; set; }
    [Inject] public IUserPersistence _UserPersistence { get; set; }
    [Inject] public NavigationManager _NavigationManager { get; set; }
    [Parameter] public string? gameCode { get; set; }
    public CricketPracticeGame Game { get; set; } = new();
    public List<CricketPracticeGamePlayerStats> PlayerStats { get; set; }
    protected override async Task OnParametersSetAsync()
    {
        if (gameCode == null)
        {
            return;
        }
        var game = await _CricketPracticeGamePersistence.Get(gameCode);
        if (game == null)
        {
            _NavigationManager.NavigateTo($"/cricket-practice-setup");
            return;
        }
        if (!game.Players.All(x => x.Throws.Count(x => x.ThirdDart != -1) == game.Targets.Count))
        {
            _NavigationManager.NavigateTo("/cricket-practice/" + gameCode);
        }
        Game = game;
        await FillPlayerStats();
    }
    public async Task RepeatGame()
    {
        string code = Guid.NewGuid().ToString();
        await _CricketPracticeGamePersistence.AddOrUpdate(
             new CricketPracticeGame
             {
                 Code = code,
                 Players = Game.Players.Select(x => new CricketPracticeGamePlayer
                 {
                     UserId = x.UserId,
                     GuestName = x.GuestName,
                     Throws = new()
                 }).ToList(),
             });
        _NavigationManager.NavigateTo("/cricket-practice/" + code);
    }
    public void NavigateToSetup()
    {
        _NavigationManager.NavigateTo($"/cricket-practice-setup");
    }
    private async Task FillPlayerStats()
    {
        var playerStats = new List<CricketPracticeGamePlayerStats>();
        foreach (var player in Game.Players ?? new())
        {
            int singleCount = player.Throws.Count(x => x.FirstDart == 1)
                            + player.Throws.Count(x => x.SecondDart == 1)
                            + player.Throws.Count(x => x.ThirdDart == 1);

            int doubleCount = player.Throws.Count(x => x.FirstDart == 2)
                            + player.Throws.Count(x => x.SecondDart == 2)
                            + player.Throws.Count(x => x.ThirdDart == 2);

            int tripleCount = player.Throws.Count(x => x.FirstDart == 3)
                            + player.Throws.Count(x => x.SecondDart == 3)
                            + player.Throws.Count(x => x.ThirdDart == 3);

            int closedNumbers = player.Throws.Count(x => x.Total > 3);

            playerStats.Add(new CricketPracticeGamePlayerStats
            {
                Name = (player.UserId != null ? (await _UserPersistence.GetUser(player.UserId.Value))?.Name : player.GuestName) ?? "Unknown",
                UserId = player.UserId,
                Average = player.Points / 7.0,
                Points = player.Points,
                ClosedNumbers = closedNumbers,
                ClosedNumbersRate = closedNumbers / 7.0 * 100,
                HitRate = (singleCount + doubleCount + tripleCount) / 21.0 * 100,
                SingleCount = singleCount,
                DoubleCount = doubleCount,
                TripleCount = tripleCount,
                SingleRate = singleCount / 21.0 * 100,
                DoubleRate = doubleCount / 21.0 * 100,
                TripleRate = tripleCount / 21.0 * 100,
            });
        }
        PlayerStats = playerStats.OrderByDescending(x => x.Points).ToList();
    }
}
