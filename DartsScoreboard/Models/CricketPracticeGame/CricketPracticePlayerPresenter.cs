namespace DartsScoreboard.Models.CricketPracticeGame;

public class CricketPracticePlayerPresenter
{
    public int? UserId { get; set; }
    public string UserName { get; set; }
    public List<CricketPracticeGamePlayerThrow> Throws { get; set; }
    public int Points => Throws.Sum(x => x.Total);
}
