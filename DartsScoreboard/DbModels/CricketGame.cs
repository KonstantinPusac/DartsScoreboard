

namespace DartsScoreboard;

public class CricketGame
{
    [System.ComponentModel.DataAnnotations.Key]
    public string Code { get; set; }
    public List<CricketPlayer> Players { get; set; }
    public bool IsGameFinished { get; set; }
}
public class CricketPlayer
{
    public int? UserId { get; set; }
    public string Name { get; set; }
    public List<CricketThrow> Throws { get; set; } = new();
    public List<CricketNumberScore> Scores { get; set; } = new()
    {
        new CricketNumberScore(){Target="20",Count=0 },
        new CricketNumberScore(){Target="19",Count=0 },
        new CricketNumberScore(){Target="18",Count=0 },
        new CricketNumberScore(){Target="17",Count=0 },
        new CricketNumberScore(){Target="16",Count=0 },
        new CricketNumberScore(){Target="15",Count=0 },
        new CricketNumberScore(){Target="BULL",Count=0 },
    };
    public int Points { get; set; }
}
public class CricketThrow
{
    public List<CricketNumberScore> Score { get; set; }

}
public class CricketDartThrow
{
    public bool IsMiss => Target == null || Count == null || Count == 0;
    public string? Target { get; set; }
    public int? Count { get; set; }
}
public class CricketNumberScore
{
    public string Target { get; set; }
    public int Count { get; set; }
}
