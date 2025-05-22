

namespace DartsScoreboard;

public class CricketGame
{
    [System.ComponentModel.DataAnnotations.Key]
    public string Code { get; set; }
    public List<CricketPlayer> Players { get; set; }
}
public class CricketPlayer
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string GuestName { get; set; }
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


    private int GetTargetValue(string target)
    {
        if (target.Equals("BULL", StringComparison.InvariantCultureIgnoreCase))
            return 25;
        if (int.TryParse(target, out int value))
            return value;
        return 0;
    }
}
public class CricketThrow
{
    public List<CricketNumberScore> Score => GetScore();
    private List<CricketNumberScore> GetScore()
    {
        if (IsEveryThrowIndiviualy)
        {
            var score = new List<CricketNumberScore>();

            if (FirstDart == null)
                return score;
            if (!FirstDart.IsMiss)
                score.Add(new CricketNumberScore { Count = FirstDart.Count!.Value, Target = FirstDart.Target! });

            if (SecondDart == null)
                return score;
            if (!SecondDart.IsMiss)
                score.Add(new CricketNumberScore { Count = SecondDart.Count!.Value, Target = SecondDart.Target! });

            if (ThirdDart == null)
                return score;
            if (!ThirdDart.IsMiss)
                score.Add(new CricketNumberScore { Count = SecondDart.Count!.Value, Target = SecondDart.Target! });
            return score;
        }
        return InputScore;
    }

    public List<CricketNumberScore> InputScore { get; set; }
    public bool IsEveryThrowIndiviualy { get; set; }

    public CricketDartThrow? FirstDart { get; set; }
    public CricketDartThrow? SecondDart { get; set; }
    public CricketDartThrow? ThirdDart { get; set; }
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
