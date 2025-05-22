namespace DartsScoreboard.Models
{
    public class Language
    {
        public string Code { get; set; }  // e.g. "en", "sr", "de"
        public string DisplayName { get; set; }  // e.g. "English", "Serbian", "German"

        public Language(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }
}
