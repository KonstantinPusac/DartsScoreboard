using DartsScoreboard.Pages;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DartsScoreboard;

public partial class Home
{
    [Inject] public IUserPersistence _UserPersistence { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] public NavigationManager NavManager { get; set; } = default!;

    private async Task OpenDialogAsync()
    {
        var options = new DialogOptions { BackgroundClass = "my-custom-class" };

        await DialogService.ShowAsync<AddPlayerDialog>("Simple Dialog", options);
    }
    private async Task NewGame()
    {
        NavManager.NavigateTo("/games");
    }
    private async Task OpenStats()
    {
        NavManager.NavigateTo("/PlayersStats");
    }
    private void Settings()
    {
        NavManager.NavigateTo("/settings");
    }
}
