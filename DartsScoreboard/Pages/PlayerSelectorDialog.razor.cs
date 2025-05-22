using IndexedDB.Blazor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DartsScoreboard;

public partial class PlayerSelectorDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; }
    public List<User> AllUsers { get; private set; } = new();
    public List<User> SelectedPlayers { get; set; } = new();
    public int GuestCounter { get; private set; } = 1;
    public bool ShowAddPopup { get; set; } = false;
    public bool ShowUserDropdown { get; set; } = false;

    private readonly IIndexedDbFactory _dbFactory;
    public PlayerSelectorDialog(IIndexedDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }
    private void Submit() => MudDialog.Close(DialogResult.Ok(SelectedPlayers));

    private void Cancel() => MudDialog.Cancel();
    private void OnUserSelected(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int id))
        {
            var user = AllUsers.FirstOrDefault(x => x.Id == id);
            if (user != null)
            {
                AddExistingPlayer(user);
            }
        }
    }

    public void OpenAddPopup()
    {
        if (SelectedPlayers.Count < 4)
            ShowAddPopup = true;
    }

    public void CloseAddPopup()
    {
        ShowAddPopup = false;
        ShowUserDropdown = false;
    }
    public void ShowExistingPlayerSelection()
    {
        ShowUserDropdown = true;
    }
    public void AddGuestPlayer()
    {
        if (SelectedPlayers.Count > 4) return;

        var guest = new User
        {
            Name = $"Guest {GuestCounter++}",
            Id = -GuestCounter          // if Id is negative its the guest player
        };
        SelectedPlayers.Add(guest);
        CloseAddPopup();
    }
    public void AddExistingPlayer(User user)
    {
        if (SelectedPlayers.Count > 4) return;
        if (SelectedPlayers.Exists(p => p.Id == user.Id)) return;

        SelectedPlayers.Add(user);
        CloseAddPopup();
    }
    public void RemovePlayer(User user)
    {
        SelectedPlayers.Remove(user);
    }
    public async Task LoadAllUsersAsync()
    {
        try
        {
            using var db = await _dbFactory.Create<DartsScoreBoardDb>();
            AllUsers = db.Users.ToList();  // Safe: store is guaranteed to exist
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IndexedDB] Failed to load users: {ex.Message}");
            AllUsers = new List<User>(); // fallback
        }
    }
}
