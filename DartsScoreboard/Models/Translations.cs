namespace DartsScoreboard
{
    public class Translations
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Language"] = "Language",
                ["Settings"] = "Settings",
                ["Keyboard"] = "Keyboard",
                ["Selected language"] = "Selected language"
            },
            ["sr"] = new Dictionary<string, string>
            {
                ["Language"] = "Jezik",
                ["Settings"] = "Podesavanja",
                ["Keyboard"] = "Tastatura",
                ["Selected language"] = "Izabrani jezik"
            }
        };

        public static string Get(string key, string languageCode)
        {
            if (_translations.TryGetValue(languageCode, out var dict) && dict.TryGetValue(key, out var value)) { return value; }
            return _translations["en"].TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
