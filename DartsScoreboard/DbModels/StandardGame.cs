namespace DartsScoreboard;

public class StandardGame
{
    [System.ComponentModel.DataAnnotations.Key]
    public string Code { get; set; } = "";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Game configuration
    public List<User> Players { get; set; } = new();
    public int StartingPoints { get; set; }
    public int NumOfSets { get; set; }
    public int NumOfLegs { get; set; }
    public string StartingIn { get; set; } = "STRAIGHT IN";
    public string StartingOut { get; set; } = "DOUBLE OUT";

    // Game state
    public int CurrentPlayerIndex { get; set; }
    public string InputScoreDartOne { get; set; } = "";
    public string InputScoreDartTwo { get; set; } = "";
    public string InputScoreDartThree { get; set; } = "";
    public int DartIndex { get; set; } = 1;
    public bool UseThreeDartMode { get; set; } = false;
    public string SelectedMultiplier { get; set; } = "S";
    public bool WinnerPopup { get; set; } = false;

    // Score tracking
    public Dictionary<int, PlayerScoreInfo> PlayerScores { get; set; } = new();
    public List<User> PlayerStats { get; set; } = new(); // for restoring averages, high scores, etc.
    public List<RoundSnapshot> UndoHistory { get; set; } = new();

    public StandardGame() { }

    public StandardGame(List<User> players, int startingPoints, int numOfLegs, int numOfSets, string startIn, string endOut)
    {
        Code = Guid.NewGuid().ToString();
        Players = players;
        StartingPoints = startingPoints;
        NumOfLegs = numOfLegs;
        NumOfSets = numOfSets;
        StartingIn = startIn;
        StartingOut = endOut;

        foreach (var player in players)
        {
            PlayerScores[player.Id] = new PlayerScoreInfo
            {
                PlayerScore = startingPoints,
                PlayerThrows = 0,
                PlayerThrowsLeg = 0,
                PlayerSets = 0,
                PlayerLegs = 0,
                PlayerCollectedScore = 0
            };
        }

        PlayerStats = players.Select(p => new User
        {
            Id = p.Id,
            Name = p.Name,
            Stats = new UserStats
            {
                HighScoreHits = new Dictionary<string, int>
                {
                    ["180"] = 0,
                    ["160+"] = 0,
                    ["140+"] = 0,
                    ["120+"] = 0,
                    ["100+"] = 0,
                    ["80+"] = 0,
                    ["60+"] = 0,
                    ["40+"] = 0
                }
            }
        }).ToList();
    }
}

