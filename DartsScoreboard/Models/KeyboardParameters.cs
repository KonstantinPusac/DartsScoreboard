namespace DartsScoreboard;

public class KeyboardParameters
{
    public List<List<KeyboardKey>> KeyboardKeys { get; set; }
}
public class KeyboardKey
{
    public required string Text { get; set; }
    public required string Value { get; set; }
    public Func<bool>? IsDisabled { get; set; }
    public Func<int>? HitCount { get; set; }
    public Func<int>? Points { get; set; }
    public Func<string>? Style { get; set; }
    public string StyleDisabled { get; set; } = "";
    public Func<KeyboardKey, Task>? OnLongPress { get; set; }
}
