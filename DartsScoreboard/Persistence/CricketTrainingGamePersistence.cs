using IndexedDB.Blazor;

namespace DartsScoreboard;
public interface ICricketPracticeGamePersistence
{
    Task AddOrUpdate(CricketPracticeGame entity);
    Task<CricketPracticeGame?> Get(string id);
    Task<List<CricketPracticeGame>> GetAll();
    Task Remove(string id);
}
public class CricketPracticeGamePersistence : ICricketPracticeGamePersistence
{
    private IIndexedDbFactory _DbFactory;

    public CricketPracticeGamePersistence(IIndexedDbFactory dbFactory)
    {
        _DbFactory = dbFactory;
    }
    public async Task AddOrUpdate(CricketPracticeGame entity)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();

        var existingEntity = db.CricketPracticeGames.FirstOrDefault(x => x.Code == entity.Code);
        if (existingEntity == null)
            db.CricketPracticeGames.Add(entity);
        else
        {
            existingEntity.Targets = entity.Targets;
            existingEntity.Players = entity.Players;
            existingEntity.Sets = entity.Sets;
            existingEntity.Legs = entity.Legs;
            existingEntity.UnlimitedLegs = entity.UnlimitedLegs;
        }
        await db.SaveChanges();
    }
    public async Task Remove(string code)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var entity = db.CricketPracticeGames.FirstOrDefault(x => x.Code == code);
        if (entity == null)
            return;
        db.CricketPracticeGames.Remove(entity);
        await db.SaveChanges();
    }
    public async Task<CricketPracticeGame?> Get(string code)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.CricketPracticeGames.FirstOrDefault(x => x.Code == code);
    }
    public async Task<List<CricketPracticeGame>> GetAll()
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.CricketPracticeGames.ToList();
    }
}
