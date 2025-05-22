namespace DartsScoreboard;

public class CricketPracticeGame
{
    [System.ComponentModel.DataAnnotations.Key]
    public string Code { get; set; }
    public List<CricketPracticeGamePlayer> Players { get; set; }
    public List<string> Targets { get; set; }
    public CricketPracticeGame()
    {
        Code = Guid.NewGuid().ToString();
        Targets = new() { "20", "19", "18", "17", "16", "15", "BULL" };
    }
}
public class CricketPracticeGamePlayer
{
    public int? UserId { get; set; }
    public string? GuestName { get; set; }
    public List<CricketPracticeGamePlayerThrow> Throws { get; set; } = new();
    public int Points => Throws.Sum(x => x.Total);
}
public class CricketPracticeGamePlayerThrow
{
    public string Number { get; set; }
    public int FirstDart { get; set; } = -1;
    public int SecondDart { get; set; } = -1;
    public int ThirdDart { get; set; } = -1;
    public int Bonus => CalculateBonus();
    public int Total => FirstDart +
                        (SecondDart == -1 ? 0 : SecondDart) +
                        (ThirdDart == -1 ? 0 : ThirdDart) +
                        (ThirdDart == -1 ? 0 : Bonus);
    public int CalculateBonus()
    {
        int bonus = 0;
        if (FirstDart == 0 && SecondDart == 0 && ThirdDart == 0)
            return -1;
        if (FirstDart * SecondDart * ThirdDart > 0)
            bonus++;

        int total = FirstDart + SecondDart + ThirdDart;
        if (total >= 3)
            bonus++;
        return bonus;
    }
    public override string ToString()
    {
        if (SecondDart == -1)
            return FirstDart.ToString();
        if (ThirdDart == -1)
            return $"{FirstDart} + {SecondDart}";
        return $"{FirstDart} + {SecondDart} + {ThirdDart}{(Bonus != 0 ? $" + ({Bonus})" : "")}";
    }
}