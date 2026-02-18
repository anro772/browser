using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

public class ChannelItemViewModelTests
{
    private static ChannelResponse MakeResponse(int members = 3, int rules = 7)
        => new ChannelResponse
        {
            Id = Guid.NewGuid(),
            Name = "Test Channel",
            Description = "A test channel",
            OwnerUsername = "alice",
            IsPublic = true,
            MemberCount = members,
            RuleCount = rules,
            CreatedAt = DateTime.UtcNow
        };

    [Fact]
    public void DisplayInfo_ShowsMemberAndRuleCount()
    {
        var vm = new ChannelItemViewModel(MakeResponse(3, 7));
        Assert.Equal("3 members • 7 rules", vm.DisplayInfo);
    }

    [Fact]
    public void DisplayInfo_SingleMemberAndZeroRules()
    {
        var vm = new ChannelItemViewModel(MakeResponse(1, 0));
        Assert.Equal("1 members • 0 rules", vm.DisplayInfo);
    }

    [Fact]
    public void Properties_MappedFromResponse()
    {
        var response = MakeResponse();
        var vm = new ChannelItemViewModel(response);

        Assert.Equal(response.Id, vm.Id);
        Assert.Equal(response.Name, vm.Name);
        Assert.Equal(response.Description, vm.Description);
        Assert.Equal(response.OwnerUsername, vm.OwnerUsername);
        Assert.Equal(response.MemberCount, vm.MemberCount);
        Assert.Equal(response.RuleCount, vm.RuleCount);
    }
}

public class JoinedChannelViewModelTests
{
    private static ChannelMembershipDto MakeDto(DateTime? lastSynced = null)
        => new ChannelMembershipDto
        {
            Id = Guid.NewGuid().ToString(),
            ChannelId = "chan-1",
            ChannelName = "My Channel",
            ChannelDescription = "Description",
            Username = "bob",
            IsActive = true,
            JoinedAt = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc),
            LastSyncedAt = lastSynced ?? new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc),
            RuleCount = 5
        };

    [Fact]
    public void LastSyncedDisplay_StartsWithLastSynced()
    {
        var vm = new JoinedChannelViewModel(MakeDto());
        Assert.StartsWith("Last synced: ", vm.LastSyncedDisplay);
    }

    [Fact]
    public void LastSyncedDisplay_ContainsFormattedDate()
    {
        var syncTime = new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc);
        var vm = new JoinedChannelViewModel(MakeDto(syncTime));
        // The "g" format includes date and time
        Assert.Contains(syncTime.ToString("g"), vm.LastSyncedDisplay);
    }

    [Fact]
    public void Properties_MappedFromDto()
    {
        var dto = MakeDto();
        var vm = new JoinedChannelViewModel(dto);

        Assert.Equal(dto.ChannelId, vm.ChannelId);
        Assert.Equal(dto.ChannelName, vm.ChannelName);
        Assert.Equal(dto.ChannelDescription, vm.ChannelDescription);
        Assert.Equal(dto.RuleCount, vm.RuleCount);
    }
}

public class ChannelsViewModelTests
{
    private readonly Mock<IChannelApiClient> _apiClientMock = new();
    private readonly Mock<IChannelSyncService> _syncServiceMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();

    private ChannelsViewModel CreateVm() =>
        new(_apiClientMock.Object, _syncServiceMock.Object, _scopeFactoryMock.Object);

    [Fact]
    public void InitialState_DefaultUsername()
    {
        var vm = CreateVm();
        Assert.Equal("default_user", vm.Username);
    }

    [Fact]
    public void InitialState_IsLoadingFalse()
    {
        var vm = CreateVm();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void InitialState_CollectionsEmpty()
    {
        var vm = CreateVm();
        Assert.Empty(vm.AvailableChannels);
        Assert.Empty(vm.JoinedChannels);
    }

    [Fact]
    public void InitialState_SearchFilterEmpty()
    {
        var vm = CreateVm();
        Assert.Equal(string.Empty, vm.SearchFilter);
    }

    [Fact]
    public void InitialState_StatusMessageEmpty()
    {
        var vm = CreateVm();
        Assert.Equal(string.Empty, vm.StatusMessage);
    }
}
