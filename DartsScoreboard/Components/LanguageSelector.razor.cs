using DartsScoreboard.Models;
using Microsoft.AspNetCore.Components;

namespace DartsScoreboard
{
    public partial class LanguageSelector
    {
        [Parameter, EditorRequired]
        public List<Language> Languages { get; set; }   // INPUT: Available laguages

        [Parameter, EditorRequired]
        public string SelectedLanguage { get; set; }    // INPUT: Selected language

        [Parameter, EditorRequired]
        public EventCallback<string> SelectedLanguageChanged { get; set; }  // OUTPUT: Notifies parent

        private async Task OnChange(ChangeEventArgs e)
        {
            var newLang = e.Value?.ToString();
            if (newLang != SelectedLanguage)
            {
                SelectedLanguage = newLang;
                await SelectedLanguageChanged.InvokeAsync(newLang);
            }
        }
    }
}
