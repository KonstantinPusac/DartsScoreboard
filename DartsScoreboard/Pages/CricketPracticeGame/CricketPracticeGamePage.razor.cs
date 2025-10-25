using DartsScoreboard.Models.CricketPracticeGame;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Numerics;

namespace DartsScoreboard;

public partial class CricketPracticeGamePage
{
    [Inject] public ICricketPracticeGamePersistence _CricketPracticeGamePersistence { get; set; }
    [Inject] public IUserPersistence _UserPersistence { get; set; }
    [Inject] public NavigationManager _NavigationManager { get; set; }
    [Parameter] public string? gameCode { get; set; }
    public CricketPracticeGame Game { get; set; } = new();
    public int Round { get; set; } = 1;
    public string CurrentTarget => Game.Targets[Round - 1];
    public List<CricketPracticePlayerPresenter> Players { get; set; } = new();
    public int PlayerOnTurnIndex { get; set; }
    private int _StackIndex = 0;
    private List<CricketPracticeGameThrowStack> _Stack = new();
    private bool _IsEndOfGame = false;
    public KeyboardParameters KeyboardParameters => CurrentTarget == "BULL" ? num2 : num3;
    KeyboardParameters num2 = new KeyboardParameters
    {
        KeyboardKeys = new List<List<KeyboardKey>>
        {
            new List<KeyboardKey>
            {
                new KeyboardKey{Text="0",Value="0"},
                new KeyboardKey{Text="1",Value="1"},
                new KeyboardKey{Text="2",Value="2"},
            }
        }
    };
    KeyboardParameters num3 = new KeyboardParameters
    {
        KeyboardKeys = new List<List<KeyboardKey>>
        {
            new List<KeyboardKey>
            {
                new KeyboardKey{Text="0",Value="0"},
                new KeyboardKey{Text="1",Value="1"},
                new KeyboardKey{Text="2",Value="2"},
                new KeyboardKey{Text = "3",Value = "3"},
            }
        }
    };
    protected override async Task OnParametersSetAsync()
    {
        if (gameCode == null)
        {
            return;
        }
        var game = await _CricketPracticeGamePersistence.Get(gameCode);
        if (game == null)
        {
            _NavigationManager.NavigateTo($"/cricket-practice-setup");
            return;
        }
        Game = game;
        int throwCount = Game.Players.Min(x => x.Throws.Count(x => x.ThirdDart != -1));
        if (Game.Targets.Count == throwCount)
        {
            await EndOfGame();
            return;
        }
        Round = throwCount + 1;
        var users = await _UserPersistence.GetAllUsers();
        Players = Game.Players.Select(x => new CricketPracticePlayerPresenter
        {
            Throws = x.Throws,
            UserId = x.UserId,
            UserName = users.FirstOrDefault(u => u.Id == x.UserId)?.Name ?? x.GuestName ?? "Guest",
        }).ToList();

        ResolvePlayerOnTurn(throwCount);
    }
    public async Task HandleKey(KeyboardKey key)
    {
        int score = int.Parse(key.Value);
        await AddThrowDart(score, false);
    }

    private async Task AddThrowDart(int score, bool isRedo)
    {
        if (_IsEndOfGame)
            return;
        try
        {
            var currentThrow = Players[PlayerOnTurnIndex].Throws.FirstOrDefault(x => x.Number == CurrentTarget);

            if (currentThrow == null || currentThrow.ThirdDart > 0)
            {
                currentThrow = new CricketPracticeGamePlayerThrow
                {
                    FirstDart = score,
                    Number = CurrentTarget,
                };
                Players[PlayerOnTurnIndex].Throws.Add(currentThrow);
                AddToStack(PlayerOnTurnIndex, Players[PlayerOnTurnIndex].Throws.Count - 1, 1, score, isRedo);
                return;
            }
            if (currentThrow.SecondDart == -1)
            {
                currentThrow.SecondDart = score;
                AddToStack(PlayerOnTurnIndex, Players[PlayerOnTurnIndex].Throws.Count - 1, 2, score, isRedo);
                return;
            }
            currentThrow.ThirdDart = score;
            AddToStack(PlayerOnTurnIndex, Players[PlayerOnTurnIndex].Throws.Count - 1, 3, score, isRedo);
            PlayerOnTurnIndex = (PlayerOnTurnIndex + 1) % Players.Count;
            if (PlayerOnTurnIndex == 0)
            {
                if (Round == (Game.Targets.Count))
                {
                    await EndOfGame();
                    return;
                }
                Round++;
            }
        }
        finally
        {
            StateHasChanged();
            await SaveGame();
        }
    }

    private void AddToStack(int playerIndex, int targetIndex, int dart, int score, bool isRedo)
    {
        if (isRedo)
        {
            _StackIndex++;
            return;
        }
        if (_Stack.Count > _StackIndex)
            _Stack.RemoveRange(_StackIndex, _Stack.Count - _StackIndex);
        _Stack.Add(new CricketPracticeGameThrowStack { Dart = dart, PlayerIndex = playerIndex, TargetIntex = targetIndex, Score = score });
        _StackIndex++;
    }
    public async Task Undo()
    {
        _IsEndOfGame = false;
        if (_StackIndex == 0)
            return;
        CricketPracticeGameThrowStack stackItem = _Stack[--_StackIndex];

        if (stackItem.Dart == 1)
        {
            Players[stackItem.PlayerIndex].Throws.RemoveAt(stackItem.TargetIntex);
            if (PlayerOnTurnIndex == 0)
            {
                PlayerOnTurnIndex = Game.Players.Count - 1;
                Round--;
            }
            else
                PlayerOnTurnIndex--;
        }
        else if (stackItem.Dart == 2)
        {
            Players[stackItem.PlayerIndex].Throws[stackItem.TargetIntex].SecondDart = -1;
        }
        else if (stackItem.Dart == 3)
        {
            Players[stackItem.PlayerIndex].Throws[stackItem.TargetIntex].ThirdDart = -1;
        }

    }
    public async Task Redo()
    {
        if (_StackIndex == _Stack.Count)
            return;
        CricketPracticeGameThrowStack stackItem = _Stack[_StackIndex++];
        await AddThrowDart(stackItem.Score, true);

    }
    private async Task EndOfGame()
    {
        _IsEndOfGame = true;
        _NavigationManager.NavigateTo($"/cricket-practice-final/{gameCode}");
    }


    private void ResolvePlayerOnTurn(int throwCount)
    {
        PlayerOnTurnIndex = -1;
        for (int i = 0; i < Game.Players.Count; i++)
        {
            if (Game.Players[i].Throws.Count(x => x.ThirdDart != -1) == throwCount)
            {
                PlayerOnTurnIndex = i;
                break;
            }
        }
        if (PlayerOnTurnIndex == -1)
        {
            PlayerOnTurnIndex = 0;
        }
    }
    private async Task SaveGame()
    {
        await _CricketPracticeGamePersistence.AddOrUpdate(Game);
    }
}