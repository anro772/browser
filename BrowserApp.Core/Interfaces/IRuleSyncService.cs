using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for synchronizing rules with the marketplace.
/// </summary>
public interface IRuleSyncService
{
    /// <summary>
    /// Downloads a rule from the marketplace and installs it locally.
    /// </summary>
    Task<Rule?> DownloadAndInstallRuleAsync(Guid marketplaceRuleId);

    /// <summary>
    /// Gets all rules that were installed from the marketplace.
    /// </summary>
    Task<IEnumerable<Rule>> GetInstalledMarketplaceRulesAsync();

    /// <summary>
    /// Uninstalls a rule that was installed from the marketplace.
    /// </summary>
    Task<bool> UninstallMarketplaceRuleAsync(string ruleId);

    /// <summary>
    /// Uploads a local rule to the marketplace.
    /// </summary>
    Task<bool> UploadRuleAsync(Rule rule, string username);

    /// <summary>
    /// Checks if the marketplace server is available.
    /// </summary>
    Task<bool> IsServerAvailableAsync();
}
