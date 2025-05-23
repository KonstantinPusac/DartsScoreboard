namespace DartsScoreboard;

public partial class KeyboardTest
{
    public string Text { get; set; } = "";
    public void KeyClick(KeyboardKey key)
    {
        Text += key.Value;
    }
    public KeyboardParameters KeyboardParameters = new KeyboardParameters
    {
        KeyboardKeys = new List<List<KeyboardKey>>
        {
            new List<KeyboardKey>
            {
                new KeyboardKey { Text = "1", Value = "1", HitCount=()=>2},
                new KeyboardKey { Text = "2", Value = "2",IsDisabled=()=>true, HitCount=() => 1, Points=() => 20 },
                new KeyboardKey { Text = "3", Value = "3",IsDisabled=()=>true },
                new KeyboardKey { Text = "4", Value = "4" , IsDisabled =() => true},
                new KeyboardKey { Text = "5", Value = "5" },
            },
            new List<KeyboardKey>
            {
                new KeyboardKey { Text = "A", Value = "A" },
                new KeyboardKey { Text = "B", Value = "B" },
                new KeyboardKey { Text = "C", Value = "C" },
                new KeyboardKey { Text = "D", Value = "D" },
                new KeyboardKey { Text = "E", Value = "E" },
                new KeyboardKey { Text = "F", Value = "F" },
                new KeyboardKey { Text = "G", Value = "G" },
                new KeyboardKey { Text = "H", Value = "H" },
                new KeyboardKey { Text = "I", Value = "I" },
                new KeyboardKey { Text = "J", Value = "J" }
            }
        }
    };
}