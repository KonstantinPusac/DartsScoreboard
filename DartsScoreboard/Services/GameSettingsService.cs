namespace DartsScoreboard.Services
{
    public class GameSettingsService
    {
        public int StartingScore { get; set; } = 501;
        public string StartInOption { get; set; } = "STRAIGHT IN";
        public string EndInOption { get; set; } = "DOUBLE OUT";
        public int Legs { get; set; } = 1;
        public int Sets { get; set; } = 1;
        public void SetGameOptions(int score, string startIn, string endOut, int legs, int sets)
        {
            StartingScore = score;
            StartInOption = startIn;
            EndInOption = endOut;
            Legs = legs;
            Sets = sets;
        }
    }
}
