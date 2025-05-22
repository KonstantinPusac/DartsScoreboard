using IndexedDB.Blazor;

namespace DartsScoreboard;

public class DbInitializerService
{
    private readonly IIndexedDbFactory _factory;

    public DbInitializerService(IIndexedDbFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureStoresCreated()
    {
        try
        {
            var db = await _factory.Create<DartsScoreBoardDb>();

            // Force create Users store
            if (!db.Users.Any())
            {
                db.Users.Add(new User { Name = "__init__" });
            }

            // Force create CricketPracticeGames store
            if (!db.CricketPracticeGames.Any())
            {
                db.CricketPracticeGames.Add(new CricketPracticeGame { Code = "__init__" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[IndexedDB] Store init error: " + ex.Message);
        }
    }
}
