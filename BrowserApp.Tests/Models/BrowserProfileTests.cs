using System.Text.Json;
using BrowserApp.Core.Models;
using Xunit;

namespace BrowserApp.Tests.Models;

public class BrowserProfileTests
{
    [Fact]
    public void Defaults_AreSet()
    {
        var p = new BrowserProfile();
        Assert.NotEqual(Guid.Empty, p.Id);
        Assert.Equal("Default", p.Name);
        Assert.Equal("#0078D4", p.Color);
        Assert.False(p.IsDefault);
        Assert.False(p.IsActive);
        Assert.True((DateTime.UtcNow - p.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void IsActive_DoesNotSerialize()
    {
        // IsActive is computed per-session in ProfileSelectorViewModel.LoadProfiles —
        // it must never round-trip through profiles.json or two sessions would fight.
        var p = new BrowserProfile { Name = "Work", IsActive = true };
        var json = JsonSerializer.Serialize(p);
        Assert.DoesNotContain("IsActive", json);
    }

    [Fact]
    public void Round_Trip_RestoresPersistedFields_NotIsActive()
    {
        var original = new BrowserProfile
        {
            Id = Guid.NewGuid(),
            Name = "Work",
            Color = "#FF8C00",
            IsDefault = true,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original);
        var clone = JsonSerializer.Deserialize<BrowserProfile>(json)!;

        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Color, clone.Color);
        Assert.Equal(original.IsDefault, clone.IsDefault);
        Assert.Equal(original.CreatedAt, clone.CreatedAt);
        Assert.False(clone.IsActive); // explicitly NOT persisted
    }
}
