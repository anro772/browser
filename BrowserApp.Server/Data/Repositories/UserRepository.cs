using Microsoft.EntityFrameworkCore;
using BrowserApp.Server.Data.Entities;
using BrowserApp.Server.Interfaces;

namespace BrowserApp.Server.Data.Repositories;

/// <summary>
/// Repository for user operations using Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ServerDbContext _context;

    public UserRepository(ServerDbContext context)
    {
        _context = context;
    }

    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<UserEntity> AddAsync(UserEntity user)
    {
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<UserEntity> GetOrCreateAsync(string username)
    {
        var existing = await GetByUsernameAsync(username);
        if (existing != null)
        {
            return existing;
        }

        var newUser = new UserEntity
        {
            Username = username,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        return newUser;
    }
}
