using IndexedDB.Blazor;

namespace DartsScoreboard;

public class PlayerSelectionService
{
    public List<User> AllUsers { get; private set; } = new();
    public List<User> SelectedPlayers { get; set; } = new();
    public int GuestCounter { get; private set; } = 1;

    public bool ShowAddPopup { get; set; } = false;
    public bool ShowUserDropdown { get; set; } = false;

    private readonly IIndexedDbFactory _dbFactory;
    public PlayerSelectionService(IIndexedDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void Reset()
    {
        SelectedPlayers.Clear();
        GuestCounter = 1;
        ShowAddPopup = false;
        ShowUserDropdown = false;
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
