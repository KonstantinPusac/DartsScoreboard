using IndexedDB.Blazor;

namespace DartsScoreboard;
public interface IStandardGamePersistence
{
    Task AddOrUpdate(StandardGame entity);
    Task<StandardGame?> Get(string id);
    Task<List<StandardGame>> GetAll();
    Task Remove(string id);
}

public class StandardGamePersistence : IStandardGamePersistence
{
    private IIndexedDbFactory _DbFactory;

    public StandardGamePersistence(IIndexedDbFactory dbFactory)
    {
        _DbFactory = dbFactory;
    }
    public async Task AddOrUpdate(StandardGame entity)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var existingEntity = db.StandardGames.FirstOrDefault(x => x.Code == entity.Code);
        if (existingEntity == null)
            db.StandardGames.Add(entity);
        else
        {
            existingEntity.Players = entity.Players;
            existingEntity.StartingPoints = entity.StartingPoints;

            existingEntity.NumOfSets = entity.NumOfSets;
            existingEntity.NumOfLegs = entity.NumOfLegs;
            existingEntity.StartingIn = entity.StartingIn;
            existingEntity.StartingOut = entity.StartingOut;

            existingEntity.CurrentPlayerIndex = entity.CurrentPlayerIndex;
            existingEntity.InputScoreDartOne = entity.InputScoreDartOne;
            existingEntity.InputScoreDartTwo = entity.InputScoreDartTwo;
            existingEntity.InputScoreDartThree = entity.InputScoreDartThree;
            existingEntity.DartIndex = entity.DartIndex;
            existingEntity.UseThreeDartMode = entity.UseThreeDartMode;
            existingEntity.SelectedMultiplier = entity.SelectedMultiplier;
            existingEntity.WinnerPopup = entity.WinnerPopup;

            existingEntity.PlayerScores = entity.PlayerScores;
            existingEntity.PlayerStats = entity.PlayerStats;
            existingEntity.UndoHistory = entity.UndoHistory;
        }
        // db.StandardGames.Add(entity);
        await db.SaveChanges();
    }
    public async Task Remove(string code)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var entity = db.StandardGames.FirstOrDefault(x => x.Code == code);
        if (entity == null)
            return;
        db.StandardGames.Remove(entity);
        await db.SaveChanges();
    }
    public async Task<StandardGame?> Get(string code)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.StandardGames.FirstOrDefault(x => x.Code == code);
    }
    public async Task<List<StandardGame>> GetAll()
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.StandardGames.ToList();
    }
}
