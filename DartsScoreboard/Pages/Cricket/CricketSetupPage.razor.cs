using DartsScoreboard.Models;
using DartsScoreboard.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;


namespace DartsScoreboard
{
    public partial class CricketSetupPage
    {
        // Player settings 
        [Inject] IDialogService _DialogService { get; set; } = default!;
        [Inject] PlayerSelectionService PlayerService { get; set; } = default!;
        [Inject] IUserPersistence _UserPersistence { get; set; } = default!;
        [Inject] NavigationManager _NavigationManager { get; set; } = default!;
        [Inject] public ICricketPersistence _CricketPersistence { get; set; } = default!;
        private bool IsFull => PlayerService.SelectedPlayers.Count >= 4;
        protected override async Task OnInitializedAsync()
        {
            // now it is safe to call anything that uses db.Users.ToList() etc.
            await PlayerService.LoadAllUsersAsync();
        }
        public async Task OpenAddPopup()
        {

            var options = new DialogOptions { CloseOnEscapeKey = true };

            var result = await _DialogService.ShowAsync<PlayerSelectorDialog>("", options);
        }
        private async Task StartGame()
        {
            PlayerService.SelectedPlayers.Clear();
            PlayerService.SelectedPlayers.Add(new User { Name = "Guest 1", });
            PlayerService.SelectedPlayers.Add(new User { Name = "Guest 2", });
            string code = Guid.NewGuid().ToString();
            await _CricketPersistence.AddOrUpdate(
                 new CricketGame
                 {
                     Code = code,
                     Players = PlayerService.SelectedPlayers.Select(x => new CricketPlayer
                     {
                         UserId = x.Id < 0 ? null : x.Id,
                         GuestName = x.Id < 0 ? x.Name : "Guest",
                         Throws = new()
                     }).ToList(),

                 });
            _NavigationManager.NavigateTo("/cricket/" + code);
        }
    }
}
