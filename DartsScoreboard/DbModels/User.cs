namespace DartsScoreboard;

public class User
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public UserStats Stats { get; set; } = new ();
    public List<OldGamesStats> GameHistory { get; set; } = new ();
}

public class UserStats
{
    // Additional statistics
    public double DartsPerLeg { get; set; }
    public double TotalDartsThrown { get; set; }
    public double TotalDoublesThrown { get; set; }

    // Basic statistics
    public double ThreeDartAverage { get; set; }
    public double ThreeDartLegAverage { get; set; }
    public double BestThreeDartLegAverage { get; set; }

    public double BestNumOfDartsThrown { get; set; }
    public double WorstNumOfDartsThrown { get; set; }

    public int NumOfDoublesThrown { get; set; }
    public double CheckoutPercentage { get; set; }

    public int HighestFinish { get; set; }
    public int HighestScore { get; set; }
    public Dictionary<string, int> HighScoreHits { get; set; } = new()
    {
        { "180", 0 },
        { "160+", 0 },
        { "140+", 0 },
        { "120+", 0 },
        { "100+", 0 },
        { "80+", 0 },
        { "60+", 0 },
        { "40+", 0 }
    };
}

public class OldGamesStats
{
    public string GameCode { get; set; }
    public int GameStartingScore { get; set; }
    public double OldThreeDartAverage { get; set; }
    public double OldCheckoutPercentage { get; set; }
    public double OldTotalDartsThrown { get; set; }
    public double OldNumOfDoublesThrown { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
