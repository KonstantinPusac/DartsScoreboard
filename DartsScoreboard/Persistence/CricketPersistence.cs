using IndexedDB.Blazor;

namespace DartsScoreboard;
public interface ICricketPersistence
{
    Task AddOrUpdate(CricketGame entity);
    Task<CricketGame?> Get(string id);
    Task<List<CricketGame>> GetAll();
    Task Remove(string id);
}
public class CricketPersistence : ICricketPersistence
{
    private IIndexedDbFactory _DbFactory;

    public CricketPersistence(IIndexedDbFactory dbFactory)
    {
        _DbFactory = dbFactory;
    }
    public async Task AddOrUpdate(CricketGame entity)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();

        var existingEntity = db.CricketGames.FirstOrDefault(x => x.Code == entity.Code);
        if (existingEntity == null)
            db.CricketGames.Add(entity);
        else
        {
            existingEntity.Players = entity.Players;
        }
        await db.SaveChanges();
    }
    public async Task Remove(string code)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var entity = db.CricketGames.FirstOrDefault(x => x.Code == code);
        if (entity == null)
            return;
        db.CricketGames.Remove(entity);
        await db.SaveChanges();
    }
    public async Task<CricketGame?> Get(string code)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.CricketGames.FirstOrDefault(x => x.Code == code);
    }
    public async Task<List<CricketGame>> GetAll()
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.CricketGames.ToList();
    }
}
