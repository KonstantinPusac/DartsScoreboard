namespace DartsScoreboard;

public class CricketPracticeGamePlayerStats
{
    public int? UserId { get; set; }
    public string Name { get; set; }
    public int Points { get; set; }
    public double Average { get; set; }
    public int ClosedNumbers { get; set; }
    public double ClosedNumbersRate { get; set; }
    public double HitRate { get; set; }
    public int SingleCount { get; set; }
    public double SingleRate { get; set; }
    public int DoubleCount { get; set; }
    public double DoubleRate { get; set; }
    public int TripleCount { get; set; }
    public double TripleRate { get; set; }
}
