using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DartsScoreboard
{
    public partial class CheckoutPracticePlay
    {
        [Parameter] public int playerId { get; set; }
        [Inject] IUserPersistence _UserPersistence { get; set; } = default!;
        [Inject] NavigationManager NavManager { get; set; } = default!;

        public User? Player { get; set; }

        public int SelectedNumber { get; set; } = 0;
        public KeyboardParameters KeyboardParams = new();

        // Input squares
        public string InputDartOne { get; set; } = "";
        public string InputDartTwo { get; set; } = "";
        public string InputDartThree { get; set; } = "";
        public int DartIndex { get; set; } = 1;

        // Stats
        public double[] CheckoutPercentage { get; set; } = new double[21];
        public int[] TotalThrows { get; set; } = new int[21];
        public int[] TotalHits { get; set; } = new int[21];

        protected override async Task OnInitializedAsync()
        {
            SelectedNumber = 1; // Default to 1 for checkout practice
            UpdateKeyboard();

            if (playerId < 0)
            {
                // Guest player
                Player = new User { Id = playerId, Name = $"Guest" };
            }
            else
            {
                Player = await _UserPersistence.GetUser(playerId);
            }
        }

        private void SubmitScore()
        {
            if (InputDartOne == "" && InputDartTwo == "" && InputDartThree == "")
            {
                TotalThrows[SelectedNumber - 1] +=3;
            }
            else if (InputDartOne == $"D{SelectedNumber}" && InputDartTwo == "" && InputDartThree == "")
            {
                TotalThrows[SelectedNumber - 1]++;
                TotalHits[SelectedNumber - 1]++;
            }
            else if (InputDartOne == "Miss" && InputDartTwo == $"D{SelectedNumber}" && InputDartThree == "")
            {
                TotalThrows[SelectedNumber - 1] += 2;
                TotalHits[SelectedNumber - 1] += 1;
            }
            else if (InputDartOne == "Miss" && InputDartTwo == "Miss" && InputDartThree == $"D{SelectedNumber}")
            {
                TotalThrows[SelectedNumber - 1] += 3;
                TotalHits[SelectedNumber - 1] += 1;
            }
            else
            {
                TotalThrows[SelectedNumber - 1] += 3;
            }

            // Calculate checkout percentage
            if (TotalThrows[SelectedNumber - 1] > 0)
            {
                CheckoutPercentage[SelectedNumber - 1] = (double)TotalHits[SelectedNumber - 1] / TotalThrows[SelectedNumber - 1] * 100;
            }
            else
            {
                CheckoutPercentage[SelectedNumber - 1] = 0.0;
            }
        }
        private string GetMisses()
        {
            if (Player == null)
                return "0.00";
            int misses = TotalThrows[SelectedNumber - 1] - TotalHits[SelectedNumber - 1];
            return misses.ToString();
        }
        private string GetPercentage()
        {
            if (Player == null)
                return "0.00%";
            return $"{CheckoutPercentage[SelectedNumber - 1]:F2}%";
        }
        private string GetHits()
        {
            if (Player == null)
                return "0.00";
            return TotalHits[SelectedNumber - 1].ToString();
        }
        private string GetDartsThrown()
        {
            if (Player == null)
                return "0.00";
            return TotalThrows[SelectedNumber - 1].ToString();
        }
        private async Task EndSession()
        {
            if (Player == null)
            {
                return;
            }
            await UpdateUserStats(Player);
            NavManager.NavigateTo($"/checkoutPractice");
        }

        private async Task UpdateUserStats(User user)
        {
            var existingUser = await _UserPersistence.GetUser(user.Id);
            if (existingUser == null)
            {
                return;
            }

            existingUser.CheckoutPracticeHistory.Add(new UserCheckoutPracticeHistory
            {
                Num1Percentage = CheckoutPercentage[0],
                Num2Percentage = CheckoutPercentage[1],
                Num3Percentage = CheckoutPercentage[2],
                Num4Percentage = CheckoutPercentage[3],
                Num5Percentage = CheckoutPercentage[4],
                Num6Percentage = CheckoutPercentage[5],
                Num7Percentage = CheckoutPercentage[6],
                Num8Percentage = CheckoutPercentage[7],
                Num9Percentage = CheckoutPercentage[8],
                Num10Percentage = CheckoutPercentage[9],
                Num11Percentage = CheckoutPercentage[10],
                Num12Percentage = CheckoutPercentage[11],
                Num13Percentage = CheckoutPercentage[12],
                Num14Percentage = CheckoutPercentage[13],
                Num15Percentage = CheckoutPercentage[14],
                Num16Percentage = CheckoutPercentage[15],
                Num17Percentage = CheckoutPercentage[16],
                Num18Percentage = CheckoutPercentage[17],
                Num19Percentage = CheckoutPercentage[18],
                Num20Percentage = CheckoutPercentage[19],
                BullPercentage = CheckoutPercentage[20],
                Timestamp = DateTime.UtcNow
            });
        }

        private void OnSelectedNumberChanged(int value)
        {
            SelectedNumber = value;
            UpdateKeyboard();
        }
        private void UpdateKeyboard()
        {
            KeyboardParams = new KeyboardParameters
            {
                KeyboardKeys = new List<List<KeyboardKey>>
                {
                    new List<KeyboardKey>
                    {
                        new() { Text = "Undo", Value = "UNDO" },
                        new() { Text = "Miss", Value = "MISS" },
                        new() { Text = $"D{SelectedNumber}", Value = "HIT" }
                    }
                }
            };
        }
        private void HandleKey(KeyboardKey key)
        {
            if (key.Value == "UNDO")
            {
                // Handle undo logic
            }
            else if (key.Value == "MISS")
            {
                // Handle miss logic
                ApplyDart("Miss");
            }
            else if (key.Value == "HIT")
            {
                // Handle score submission
                ApplyDart($"D{SelectedNumber}");
            }
        }

        private void ApplyDart(string value)
        {
            if (DartIndex == 1)
            {
                InputDartOne = value;
                if (InputDartOne == $"D{SelectedNumber}")
                {
                    SubmitScore();
                    // Reset
                    InputDartOne = "";
                    DartIndex = 1; 
                }
                else
                {
                    DartIndex++;
                }
            }
            else if (DartIndex == 2)
            {
                InputDartTwo = value;
                if (InputDartTwo == $"D{SelectedNumber}")
                {
                    SubmitScore();
                    // Reset
                    InputDartOne = "";
                    InputDartTwo = "";
                    DartIndex = 1;
                }
                else
                {
                    DartIndex++;
                }
            }
            else if (DartIndex == 3)
            {
                InputDartThree = value;
                SubmitScore();

                // Reset
                InputDartOne = "";
                InputDartTwo = "";
                InputDartThree = "";
                DartIndex = 1; // Reset to first dart
            }
        }
    }
}
