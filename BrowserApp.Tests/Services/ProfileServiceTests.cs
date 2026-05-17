using System.Reflection;
using BrowserApp.Core.Models;
using BrowserApp.UI.Services;
using Xunit;

namespace BrowserApp.Tests.Services;

/// <summary>
/// ProfileService tests for the rename + color update wrappers. The on-disk
/// SaveProfiles call writes to %LOCALAPPDATA% which we don't want to touch in
/// unit tests, so we use reflection to seed the in-memory profile list and
/// rely on SaveProfiles' built-in try/catch to swallow the write failure.
/// </summary>
public class ProfileServiceTests
{
    private static ProfileService NewServiceWithProfile(BrowserProfile profile)
    {
        var svc = new ProfileService();
        var field = typeof(ProfileService).GetField("_profiles", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(svc, new List<BrowserProfile> { profile });
        return svc;
    }

    [Fact]
    public void UpdateProfileName_HappyPath_RenamesInMemory()
    {
        var p = new BrowserProfile { Name = "Old" };
        var svc = NewServiceWithProfile(p);

        var ok = svc.UpdateProfileName(p.Id, "New Name");

        Assert.True(ok);
        Assert.Equal("New Name", p.Name);
    }

    [Fact]
    public void UpdateProfileName_TrimsWhitespace()
    {
        var p = new BrowserProfile { Name = "Old" };
        var svc = NewServiceWithProfile(p);

        svc.UpdateProfileName(p.Id, "   Spaced Name   ");

        Assert.Equal("Spaced Name", p.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateProfileName_RejectsEmpty(string? input)
    {
        var p = new BrowserProfile { Name = "Original" };
        var svc = NewServiceWithProfile(p);

        var ok = svc.UpdateProfileName(p.Id, input!);

        Assert.False(ok);
        Assert.Equal("Original", p.Name);
    }

    [Fact]
    public void UpdateProfileName_UnknownId_ReturnsFalse()
    {
        var p = new BrowserProfile { Name = "Original" };
        var svc = NewServiceWithProfile(p);

        var ok = svc.UpdateProfileName(Guid.NewGuid(), "Whatever");

        Assert.False(ok);
        Assert.Equal("Original", p.Name);
    }

    [Fact]
    public void UpdateProfileColor_HappyPath_UpdatesInMemory()
    {
        var p = new BrowserProfile { Color = "#0078D4" };
        var svc = NewServiceWithProfile(p);

        var ok = svc.UpdateProfileColor(p.Id, "#FF8C00");

        Assert.True(ok);
        Assert.Equal("#FF8C00", p.Color);
    }

    [Fact]
    public void UpdateProfileColor_UnknownId_ReturnsFalse()
    {
        var p = new BrowserProfile();
        var svc = NewServiceWithProfile(p);

        Assert.False(svc.UpdateProfileColor(Guid.NewGuid(), "#FF0000"));
    }
}
