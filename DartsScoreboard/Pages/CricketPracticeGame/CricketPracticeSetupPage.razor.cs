using DartsScoreboard.Models;
using DartsScoreboard.Services;
using Microsoft.AspNetCore.Components;


namespace DartsScoreboard
{
    public partial class CricketPracticeSetupPage
    {
        // Player settings 
        [Inject] PlayerSelectionService PlayerService { get; set; } = default!;
        [Inject] IUserPersistence _UserPersistence { get; set; } = default!;
        [Inject] NavigationManager _NavigationManager { get; set; } = default!;
        [Inject] public ICricketPracticeGamePersistence _CricketPracticeGamePersistence { get; set; } = default!;
        private bool IsFull => PlayerService.SelectedPlayers.Count >= 4;
        protected override async Task OnInitializedAsync()
        {
            // now it is safe to call anything that uses db.Users.ToList() etc.
            await PlayerService.LoadAllUsersAsync();
        }

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
        private async Task StartGame()
        {

            string code = Guid.NewGuid().ToString();
            await _CricketPracticeGamePersistence.AddOrUpdate(
                 new CricketPracticeGame
                 {
                     Code = code,
                     Players = PlayerService.SelectedPlayers.Select(x => new CricketPracticeGamePlayer
                     {
                         UserId = x.Id < 0 ? null : x.Id,
                         GuestName = x.Id < 0 ? x.Name : "Guest",
                         Throws = new()
                     }).ToList(),

                 });
            _NavigationManager.NavigateTo("/cricket-practice/" + code);
        }
    }
}
