using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace DartsScoreboard
{
    public partial class Games
    {
        [Inject] public NavigationManager navManager { get; set; } = default!;
        private void Standard()
        {
            navManager.NavigateTo("/gamesStandard");
        }
        private async Task Back()
        {
            navManager.NavigateTo("/");
        }
        private async Task CricketPractice()
        {
            navManager.NavigateTo("/cricket-practice-setup");
        }
        private async Task Cricket()
        {
            navManager.NavigateTo("/cricket-setup");
        }
        private void CheckoutPractice()
        {
            navManager.NavigateTo("/checkoutPractice");
        }
    }
}
