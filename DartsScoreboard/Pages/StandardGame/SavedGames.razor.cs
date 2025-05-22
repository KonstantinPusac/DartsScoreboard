using DartsScoreboard;
using Microsoft.AspNetCore.Components;

namespace DartsScoreboard
{
    public partial class SavedGames : ComponentBase
    {
        [Inject] public IStandardGamePersistence _StandardGamePersistence { get; set; } = default!;
        [Inject] public NavigationManager NavManager { get; set; } = default!;

        public List<StandardGame> SavedGamesList { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            //SavedGamesList = await _StandardGamePersistence.GetAll();
            SavedGamesList = (await _StandardGamePersistence.GetAll())
                .OrderByDescending(g => g.LastModified)
                .ToList();
        }

        private void ResumeGame(string code)
        {
            NavManager.NavigateTo($"/gameStandardPlay/{code}");
        }

        private async Task DeleteGame(string code)
        {
            await _StandardGamePersistence.Remove(code);
            SavedGamesList = await _StandardGamePersistence.GetAll(); // refresh list
        }
    }
}