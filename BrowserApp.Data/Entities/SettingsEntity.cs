using System.ComponentModel.DataAnnotations;

namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity for key-value settings storage.
/// </summary>
public class SettingsEntity
{
    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
