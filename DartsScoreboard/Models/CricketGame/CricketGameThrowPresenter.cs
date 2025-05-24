namespace DartsScoreboard;

public class CricketGameThrowPresenter
{
    public CricketPlayerPresenter Player { get; set; }
    public List<CricketThrowScore> Score { get; set; } = new();
    public List<PointsForPlayer> PointsForPlayers { get; set; } = new();
}
