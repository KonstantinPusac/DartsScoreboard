using Microsoft.AspNetCore.Components;

namespace DartsScoreboard;

public partial class Keyboard
{
    [Parameter, EditorRequired]
    public KeyboardParameters KeyboardParameters { get; set; }
    [Parameter, EditorRequired]
    public EventCallback<KeyboardKey> KeyClick { get; set; }
}