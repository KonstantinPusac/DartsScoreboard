namespace DartsScoreboard;

public class CricketGameThrowPresenter
{
    public List<CricketThrowScore> Score { get; set; } = new();
    public List<PointsForPlayer> PointsForPlayers { get; set; } = new();
    public void Clear()
    {
        Score.Clear();
        PointsForPlayers.Clear();
    }
}
