using IndexedDB.Blazor;
using System;
using System.Data;

namespace DartsScoreboard;

public interface IUserPersistence
{
    Task Update(User user);
    Task AddUser(User user);
    Task<List<User>> GetAllUsers();
    Task<User?> GetUser(int id);
    Task RemoveUser(int id);
    Task UpdateUser(User user);
}

public class UserPersistence : IUserPersistence
{
    private IIndexedDbFactory _DbFactory;

    public UserPersistence(IIndexedDbFactory dbFactory)
    {
        _DbFactory = dbFactory;
    }
    public async Task Update(User user)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var dbUser = db.Users.FirstOrDefault(x => x.Id == user.Id);
        if (dbUser == null)
            return;
        dbUser.Name = user.Name;
        dbUser.Stats = user.Stats;
        dbUser.GameHistory = user.GameHistory;
        await db.SaveChanges();
    }
    public async Task AddUser(User user)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        db.Users.Add(user);
        await db.SaveChanges();
    }
    public async Task RemoveUser(int id)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var user = db.Users.FirstOrDefault(x => x.Id == id);
        if (user == null)
            return;
        db.Users.Remove(user);
        await db.SaveChanges();
    }
    public async Task<User?> GetUser(int id)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.Users.FirstOrDefault(x => x.Id == id);
    }
    public async Task<List<User>> GetAllUsers()
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        return db.Users.ToList();
    }
    public async Task UpdateUser(User user)
    {
        using var db = await _DbFactory.Create<DartsScoreBoardDb>();
        var dbUser = db.Users.FirstOrDefault(x => x.Id == user.Id);
        if (dbUser == null)
            return;
        dbUser.Name = user.Name;
        await db.SaveChanges();
    }
}
