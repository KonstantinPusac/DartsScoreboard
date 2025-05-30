using Microsoft.AspNetCore.Components;
using MudBlazor;
using DartsScoreboard.Models;
using DartsScoreboard.Services;

namespace DartsScoreboard
{
    public partial class PlayerStats
    {
        [Parameter] public int playerId { get; set; }
        [Inject] public NavigationManager NavManager { get; set; } = default!;
        [Inject] public IUserPersistence _UserPersistence { get; set; } = default!;

        public User? Player { get; set; }

        // Select slider
        private string _startingScoreValue = "501";
        private string _daysValue = "90";

        // Stats
        public double TotalThreeDartAverage = 0.0;
        public double TotalCheckoutPercentage = 0.0;
        public double TotalDartsPerLeg = 0.0;
        public double BestThreeDartLegAverage = 0.0;
        public double TotalDoublesThrown = 0.0;

        // Chart data
        private string[] ChartLabels = Array.Empty<string>();       // X-axis labels for the chart
        public List<ChartSeries> ChartData { get; set; } = new();   // Y-axis series

        private int _index = -1;
        private ChartOptions _options = new ChartOptions();
        private AxisChartOptions _axisChartOptions = new AxisChartOptions();

        protected override async Task OnInitializedAsync()
        {
            Player = await _UserPersistence.GetUser(playerId);

            // Initialize the stats
            _daysValue = "90";
            RecalculateStats();
            GenerateThreeDartAverageChart(90, 501);
        }

        private void AllGamesPage()
        {
            NavManager.NavigateTo($"/PlayerAllGames/{playerId}");
        }

        private void GoBack()
        {
            NavManager.NavigateTo("/PlayersStats");
        }

        private void GenerateThreeDartAverageChart(int days, int startingScore)
        {
            if (Player?.GameHistory == null || !Player.GameHistory.Any())
                return;

            List<OldGamesStats> games;

            if (days > 0)
            {
                var fromDate = DateTime.UtcNow.AddDays(-days);
                games = Player.GameHistory
                    .Where(g => g.Timestamp >= fromDate && g.GameStartingScore == startingScore)
                    .OrderBy(g => g.Timestamp)
                    .ToList();
            }
            else
            {
                // All time: take all games
                games = Player.GameHistory
                    .Where(g => g.GameStartingScore == startingScore)
                    .OrderBy(g => g.Timestamp)
                    .ToList();
            }

            if (!games.Any())
            {
                ChartLabels = Array.Empty<string>();
                ChartData = new List<ChartSeries>(); // clear chart
                return;
            }

            var averages = games
                .Select(g => g.OldThreeDartAverage)
                .ToArray();

            int total = games.Count;
            string[] labels;

            if (total <= 5)
            {
                labels = games
                    .Select(g => g.Timestamp.ToLocalTime().ToString("dd/MM"))
                    .ToArray();
            }
            else
            {
                labels = new string[total];

                for (int i = 0; i < 5; i++)
                {
                    int index = (int)Math.Round(i * (total - 1) / 4.0);
                    labels[index] = games[index].Timestamp.ToLocalTime().ToString("dd/MM");
                }

                for (int i = 0; i < total; i++)
                {
                    if (labels[i] == null)
                        labels[i] = "";
                }
            }

            ChartLabels = labels;

            ChartData = new List<ChartSeries>
            {
                new ChartSeries
                {
                    Name = days == 0 ? "3 dart avg (All Time)" : $"3 dart avg ({days}d)",
                    Data = averages
                }
            };
        }
        private void OnScoreChanged(string selectedValue)
        {
            _startingScoreValue = selectedValue;
            if (int.TryParse(selectedValue, out int score) && int.TryParse(_daysValue, out int days))
            {
                GenerateThreeDartAverageChart(days, score); // e.g. _daysFilter = 7
            }
        }

        private void OnDateChanged(string value)
        {
            _daysValue = value;
            RecalculateStats();
        }
        private void RecalculateStats()
        {
            if (_daysValue == "7")
            {
                // Logic for 7 days
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

                if (Player == null || Player.GameHistory == null)
                {
                    return;
                }

                var last7DaysGames = Player.GameHistory.Where(g => g.Timestamp >= sevenDaysAgo).ToList();

                double averageUp = 0;
                double averageDown = 0;
                double checkoutUp = 0;
                double checkoutDown = 0;
                double totalDartsThrown = 0;
                double numberOfGames = last7DaysGames.Count;

                foreach (var game in last7DaysGames)
                {
                    averageUp += game.OldThreeDartAverage * game.OldTotalDartsThrown;
                    averageDown += game.OldTotalDartsThrown;

                    checkoutUp += (game.OldCheckoutPercentage / 100.0) * game.OldNumOfDoublesThrown;
                    checkoutDown += game.OldNumOfDoublesThrown;

                    totalDartsThrown += game.OldTotalDartsThrown;

                    // Best three dart leg average
                    if (game.OldThreeDartAverage > BestThreeDartLegAverage)
                    {
                        BestThreeDartLegAverage = game.OldThreeDartAverage;
                    }
                }

                TotalThreeDartAverage = averageDown > 0 ? averageUp / averageDown : 0;
                TotalCheckoutPercentage = checkoutDown > 0 ? (checkoutUp / checkoutDown) * 100.0 : 0;
                TotalDartsPerLeg = numberOfGames > 0 ? totalDartsThrown / numberOfGames : 0;

                _startingScoreValue = "501";
                GenerateThreeDartAverageChart(7, 501);
            }
            else if (_daysValue == "30")
            {
                // Logic for 30 days
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                if (Player == null || Player.GameHistory == null)
                {
                    return;
                }
                var last30DaysGames = Player.GameHistory
                    .Where(g => g.Timestamp >= thirtyDaysAgo)
                    .ToList();
                double averageUp = 0;
                double averageDown = 0;
                double checkoutUp = 0;
                double checkoutDown = 0;
                double totalDartsThrown = 0;
                double numberOfGames = last30DaysGames.Count;
                foreach (var game in last30DaysGames)
                {
                    averageUp += game.OldThreeDartAverage * game.OldTotalDartsThrown;
                    averageDown += game.OldTotalDartsThrown;

                    checkoutUp += (game.OldCheckoutPercentage / 100.0) * game.OldNumOfDoublesThrown;
                    checkoutDown += game.OldNumOfDoublesThrown;

                    totalDartsThrown += game.OldTotalDartsThrown;
                    // Best three dart leg average
                    if (game.OldThreeDartAverage > BestThreeDartLegAverage)
                    {
                        BestThreeDartLegAverage = game.OldThreeDartAverage;
                    }
                }
                TotalThreeDartAverage = averageDown > 0 ? averageUp / averageDown : 0;
                TotalCheckoutPercentage = checkoutDown > 0 ? (checkoutUp / checkoutDown) * 100.0 : 0;
                TotalDartsPerLeg = numberOfGames > 0 ? totalDartsThrown / numberOfGames : 0;

                _startingScoreValue = "501";
                GenerateThreeDartAverageChart(30, 501);
            }
            else if (_daysValue == "90")
            {
                // Logic for 90 days
                var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
                if (Player == null || Player.GameHistory == null)
                {
                    return;
                }
                var last90DaysGames = Player.GameHistory
                    .Where(g => g.Timestamp >= ninetyDaysAgo)
                    .ToList();
                double averageUp = 0;
                double averageDown = 0;
                double checkoutUp = 0;
                double checkoutDown = 0;
                double totalDartsThrown = 0;
                double numberOfGames = last90DaysGames.Count;
                foreach (var game in last90DaysGames)
                {
                    averageUp += game.OldThreeDartAverage * game.OldTotalDartsThrown;
                    averageDown += game.OldTotalDartsThrown;

                    checkoutUp += (game.OldCheckoutPercentage / 100.0) * game.OldNumOfDoublesThrown;
                    checkoutDown += game.OldNumOfDoublesThrown;

                    totalDartsThrown += game.OldTotalDartsThrown;
                    // Best three dart leg average
                    if (game.OldThreeDartAverage > BestThreeDartLegAverage)
                    {
                        BestThreeDartLegAverage = game.OldThreeDartAverage;
                    }
                }
                TotalThreeDartAverage = averageDown > 0 ? averageUp / averageDown : 0;
                TotalCheckoutPercentage = checkoutDown > 0 ? (checkoutUp / checkoutDown) * 100.0 : 0;
                TotalDartsPerLeg = numberOfGames > 0 ? totalDartsThrown / numberOfGames : 0;

                _startingScoreValue = "501";
                GenerateThreeDartAverageChart(90, 501);
            }
            else if (_daysValue == "365")
            {
                // Logic for 1 year
                var oneYearAgo = DateTime.UtcNow.AddDays(-365);
                if (Player == null || Player.GameHistory == null)
                {
                    return;
                }
                var lastYearGames = Player.GameHistory
                    .Where(g => g.Timestamp >= oneYearAgo)
                    .ToList();
                double averageUp = 0;
                double averageDown = 0;
                double checkoutUp = 0;
                double checkoutDown = 0;
                double totalDartsThrown = 0;
                double numberOfGames = lastYearGames.Count;
                foreach (var game in lastYearGames)
                {
                    averageUp += game.OldThreeDartAverage * game.OldTotalDartsThrown;
                    averageDown += game.OldTotalDartsThrown;

                    checkoutUp += (game.OldCheckoutPercentage / 100.0) * game.OldNumOfDoublesThrown;
                    checkoutDown += game.OldNumOfDoublesThrown;

                    totalDartsThrown += game.OldTotalDartsThrown;
                    // Best three dart leg average
                    if (game.OldThreeDartAverage > BestThreeDartLegAverage)
                    {
                        BestThreeDartLegAverage = game.OldThreeDartAverage;
                    }
                }
                TotalThreeDartAverage = averageDown > 0 ? averageUp / averageDown : 0;
                TotalCheckoutPercentage = checkoutDown > 0 ? (checkoutUp / checkoutDown) * 100.0 : 0;
                TotalDartsPerLeg = numberOfGames > 0 ? totalDartsThrown / numberOfGames : 0;

                _startingScoreValue = "501";
                GenerateThreeDartAverageChart(365, 501);
            }
            else if (_daysValue == "allTime")
            {
                // Logic for all time
                if (Player == null || Player.GameHistory == null)
                {
                    return;
                }
                var allTimeGames = Player.GameHistory.ToList();
                double averageUp = 0;
                double averageDown = 0;
                double checkoutUp = 0;
                double checkoutDown = 0;
                double totalDartsThrown = 0;
                double numberOfGames = allTimeGames.Count;
                foreach (var game in allTimeGames)
                {
                    averageUp += game.OldThreeDartAverage * game.OldTotalDartsThrown;
                    averageDown += game.OldTotalDartsThrown;

                    checkoutUp += (game.OldCheckoutPercentage / 100.0) * game.OldNumOfDoublesThrown;
                    checkoutDown += game.OldNumOfDoublesThrown;

                    totalDartsThrown += game.OldTotalDartsThrown;
                    // Best three dart leg average
                    if (game.OldThreeDartAverage > BestThreeDartLegAverage)
                    {
                        BestThreeDartLegAverage = game.OldThreeDartAverage;
                    }
                }
                TotalThreeDartAverage = averageDown > 0 ? averageUp / averageDown : 0;
                TotalCheckoutPercentage = checkoutDown > 0 ? (checkoutUp / checkoutDown) * 100.0 : 0;
                TotalDartsPerLeg = numberOfGames > 0 ? totalDartsThrown / numberOfGames : 0;

                _startingScoreValue = "501";
                GenerateThreeDartAverageChart(0, 501); // 0 for all time
            }
        }
    }
}
