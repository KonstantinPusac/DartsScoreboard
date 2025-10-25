using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Charts;
using System.Runtime.Intrinsics.Arm;

namespace DartsScoreboard;

public static class CricketIcons
{
    public const string OneMark =
@"< svg width = ""16"" height = ""16"" viewBox = ""0 0 16 16"" >
        < line x1 = ""3"" y1 = ""13"" x2 = ""13"" y2 = ""3"" stroke = ""currentColor"" stroke - width = ""2"" />
    </ svg >";
    public const string TwoMarks =
@"< svg width = ""16"" height = ""16"" viewBox = ""0 0 16 16"" >
        < line x1 = ""3"" y1 = ""13"" x2 = ""13"" y2 = ""3"" stroke = ""currentColor"" stroke - width = ""2"" />
        < line x1 = ""3"" y1 = ""3"" x2 = ""13"" y2 = ""13"" stroke = ""currentColor"" stroke - width = ""2"" />
    </ svg >";
    public const string ThreeMarks =
@"< svg width = ""16"" height = ""16"" viewBox = ""0 0 16 16"" >
        < line x1 = ""3"" y1 = ""13"" x2 = ""13"" y2 = ""3"" stroke = ""currentColor"" stroke - width = ""2"" />
        < line x1 = ""3"" y1 = ""3"" x2 = ""13"" y2 = ""13"" stroke = ""currentColor"" stroke - width = ""2"" />
        < circle cx = ""8"" cy = ""8"" r = ""5"" stroke = ""currentColor"" stroke - width = ""2"" fill = ""none"" />
    </ svg >";
}
