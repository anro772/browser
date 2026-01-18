using BrowserApp.Server.Data.Entities;

namespace BrowserApp.Server.Interfaces;

/// <summary>
/// Repository interface for user operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<UserEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    Task<UserEntity?> GetByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<UserEntity> AddAsync(UserEntity user);

    /// <summary>
    /// Gets or creates a user by username.
    /// </summary>
    Task<UserEntity> GetOrCreateAsync(string username);
}
