using System.Security.Cryptography;
using System.Text;

namespace BrowserApp.Server.Utilities;

/// <summary>
/// Utility for hashing and verifying passwords using SHA256.
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hashes a password using SHA256.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>Lowercase hex string of the SHA256 hash.</returns>
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns>True if the password matches the hash.</returns>
    public static bool VerifyPassword(string password, string hash)
    {
        var inputHash = HashPassword(password);
        return inputHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}
