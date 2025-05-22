namespace DartsScoreboard;

public class CricketPlayerPresenter
{
    public string Name { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int Points { get; set; }
    public List<CricketThrow> Throws { get; set; } = new();
    public List<CricketNumberScore> Scores { get; set; } = new();
    public double Average => Scores.Sum(x => x.Count) / Throws.Count();
    public int Marks => Scores.Sum(x => x.Count);
}
