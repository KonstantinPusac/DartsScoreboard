using IndexedDB.Blazor;

namespace DartsScoreboard;

public class PlayerSelectionService
{
    public List<User> AllUsers { get; private set; } = new();
    public List<User> SelectedPlayers { get; set; } = new();
    public List<User> NotSelectedPlayers { get; set; } = new();
    public int GuestCounter { get; private set; } = 1;

    public bool ShowAddPopup { get; set; } = false;
    public bool ShowUserDropdown { get; set; } = false;

    private readonly IIndexedDbFactory _dbFactory;

    DartsScoreBoardDb db;
    public PlayerSelectionService(IIndexedDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void Reset()
    {
        SelectedPlayers.Clear();
        foreach (var item in db.Users.ToList())
        {
            item.IsSelected = false;
        }
        db.SaveChanges();
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
        db.SaveChanges();
        CloseAddPopup();
    }
    public void AddExistingPlayer(User user)
    {
        if (SelectedPlayers.Count > 4) return;
        if (SelectedPlayers.Exists(p => p.Id == user.Id)) return;

        SelectedPlayers.Add(user);
        NotSelectedPlayers.Remove(user);
        user.IsSelected = true;
        db.SaveChanges();

        CloseAddPopup();
    }
    public async Task CreateUser(User user)
    {
        var existing = SelectedPlayers.FirstOrDefault(p => p.Name == user.Name);
        if (existing != null)
        {
            existing.IsSelected = true;
            SelectedPlayers.Add(existing);
            await db.SaveChanges();
            return;
        }

        user.IsSelected = true;
        db.Users.Add(user);
        await db.SaveChanges();
        AllUsers.Add(user);
        SelectedPlayers.Add(user);
    }
    public void RemovePlayer(User user)
    {
        user.IsSelected = false;
        SelectedPlayers.Remove(user);
        db.SaveChanges();
        NotSelectedPlayers.Add(user);
    }
    public async Task LoadAllUsersAsync()
    {
        if (AllUsers.Count > 0) return;
        try
        {
            db = await _dbFactory.Create<DartsScoreBoardDb>();
            AllUsers = db.Users.ToList();  // Safe: store is guaranteed to exist
            SelectedPlayers = AllUsers.Where(x => x.IsSelected).ToList();
            NotSelectedPlayers = AllUsers.Where(x => !x.IsSelected).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IndexedDB] Failed to load users: {ex.Message}");
            AllUsers = new List<User>(); // fallback
        }
    }
}
