using DartsScoreboard.Models;
using DartsScoreboard.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace DartsScoreboard
{
    public partial class CheckoutPractice
    {
        [Inject] NavigationManager NavManager { get; set; } = default!;
        [Inject] PlayerSelectionService PlayerService { get; set; } = default!;
        [Inject] ISnackbar snackbar { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await PlayerService.LoadAllUsersAsync();
        }

        // Adding players
        private bool IsFull => PlayerService.SelectedPlayers.Count >= 1;
        private void OnUserSelected(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int id))
            {
                var user = PlayerService.AllUsers.FirstOrDefault(x => x.Id == id);
                if (user != null)
                {
                    PlayerService.AddExistingPlayer(user);
                }
            }
        }

        // Start game
        private void StartGame()
        {
            if (PlayerService.SelectedPlayers.Count < 1)
            {
                snackbar.Add("Please select at least one player to start the game.", Severity.Error);
                return;
            }
            // Navigate to the checkout practice game page
            var playerId = PlayerService.SelectedPlayers.First().Id;
            NavManager.NavigateTo($"/checkoutPracticePlay/{playerId}");
        }
    }
}
