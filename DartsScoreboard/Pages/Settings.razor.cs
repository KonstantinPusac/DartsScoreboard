using DartsScoreboard.Models;
using Microsoft.AspNetCore.Components;

namespace DartsScoreboard
{
    public partial class Settings
    {
        public string SelectedLanguage { get; set; } = "en";    // default

        public List<Language> AvailableLanguages { get; set; } = new()
        {
        new Language("en", "English"),
        new Language("sr", "Serbian")
        };

        public void OnLanguageChanged(string newLang)
        {
            SelectedLanguage = newLang;
            StateHasChanged();
        }
    }
}
