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
    private readonly Mock<IRuleEngine> _ruleEngineMock = new();

    private ChannelsViewModel CreateVm() =>
        new(_apiClientMock.Object, _syncServiceMock.Object, _scopeFactoryMock.Object, _ruleEngineMock.Object);

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

    [Fact]
    public void InitialState_UnifiedChannelsEmpty()
    {
        var vm = CreateVm();
        Assert.Empty(vm.Channels);
    }

    [Fact]
    public void InitialState_ShowJoinedOnlyFalse()
    {
        var vm = CreateVm();
        Assert.False(vm.ShowJoinedOnly);
    }

    [Fact]
    public void InitialState_CreatePanelHidden()
    {
        var vm = CreateVm();
        Assert.False(vm.IsCreatePanelVisible);
    }
}

public class UnifiedChannelViewModelTests
{
    private static ChannelResponse MakeResponse(int members = 5, int rules = 3)
        => new ChannelResponse
        {
            Id = Guid.NewGuid(),
            Name = "Privacy Rules",
            Description = "Blocks trackers",
            OwnerUsername = "alice",
            IsPublic = true,
            MemberCount = members,
            RuleCount = rules,
            CreatedAt = DateTime.UtcNow
        };

    private static ChannelMembershipDto MakeMembership(string channelId)
        => new ChannelMembershipDto
        {
            Id = Guid.NewGuid().ToString(),
            ChannelId = channelId,
            ChannelName = "Privacy Rules",
            ChannelDescription = "Blocks trackers",
            Username = "bob",
            IsActive = true,
            JoinedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            LastSyncedAt = new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc),
            RuleCount = 2
        };

    [Fact]
    public void Properties_MappedFromResponse_WhenNotJoined()
    {
        var response = MakeResponse(5, 3);
        var vm = new UnifiedChannelViewModel(response);

        Assert.Equal(response.Id, vm.Id);
        Assert.Equal("Privacy Rules", vm.Name);
        Assert.Equal("Blocks trackers", vm.Description);
        Assert.Equal("alice", vm.OwnerUsername);
        Assert.Equal(5, vm.MemberCount);
        Assert.Equal(3, vm.RuleCount);
        Assert.False(vm.IsJoined);
        Assert.Null(vm.LocalChannelId);
    }

    [Fact]
    public void Properties_MappedFromResponse_WhenJoined()
    {
        var response = MakeResponse();
        var membership = MakeMembership(response.Id.ToString());
        var vm = new UnifiedChannelViewModel(response, membership);

        Assert.True(vm.IsJoined);
        Assert.Equal(response.Id.ToString(), vm.LocalChannelId);
        Assert.Equal(2, vm.LocalRuleCount);
        Assert.NotNull(vm.JoinedAt);
        Assert.NotNull(vm.LastSyncedAt);
    }

    [Fact]
    public void DisplayInfo_ShowsMemberAndRuleCount()
    {
        var vm = new UnifiedChannelViewModel(MakeResponse(10, 4));
        Assert.Equal("10 members \u2022 4 rules", vm.DisplayInfo);
    }

    [Fact]
    public void OwnerDisplay_ShowsOwner()
    {
        var vm = new UnifiedChannelViewModel(MakeResponse());
        Assert.Equal("by alice", vm.OwnerDisplay);
    }

    [Fact]
    public void JoinedInfo_EmptyWhenNotJoined()
    {
        var vm = new UnifiedChannelViewModel(MakeResponse());
        Assert.Equal(string.Empty, vm.JoinedInfo);
    }

    [Fact]
    public void JoinedInfo_ShowsSyncDateWhenJoined()
    {
        var response = MakeResponse();
        var membership = MakeMembership(response.Id.ToString());
        var vm = new UnifiedChannelViewModel(response, membership);
        Assert.StartsWith("Last synced: ", vm.JoinedInfo);
    }

    [Fact]
    public void IsExpanded_DefaultFalse()
    {
        var vm = new UnifiedChannelViewModel(MakeResponse());
        Assert.False(vm.IsExpanded);
        Assert.Empty(vm.RulePreview);
    }
}

public class RulePreviewItemTests
{
    private static readonly Guid TestId = Guid.NewGuid();
    private static readonly Guid TestChannelId = Guid.NewGuid();

    [Fact]
    public void RecordEquality_Works()
    {
        var a = new RulePreviewItem(TestId, TestChannelId, "Block Ads", "*.example.com", true);
        var b = new RulePreviewItem(TestId, TestChannelId, "Block Ads", "*.example.com", true);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Properties_SetCorrectly()
    {
        var item = new RulePreviewItem(TestId, TestChannelId, "Dark Mode", "*", false);
        Assert.Equal(TestId, item.Id);
        Assert.Equal(TestChannelId, item.ChannelId);
        Assert.Equal("Dark Mode", item.Name);
        Assert.Equal("*", item.Site);
        Assert.False(item.IsEnforced);
    }
}

public class UnifiedChannelViewModel_IsOwnerTests
{
    [Fact]
    public void IsOwner_DefaultFalse()
    {
        var response = new ChannelResponse
        {
            Id = Guid.NewGuid(), Name = "Test", Description = "",
            OwnerUsername = "alice", IsPublic = true, MemberCount = 1,
            RuleCount = 0, CreatedAt = DateTime.UtcNow
        };
        var vm = new UnifiedChannelViewModel(response);
        Assert.False(vm.IsOwner);
    }

    [Fact]
    public void IsOwner_CanBeSet()
    {
        var response = new ChannelResponse
        {
            Id = Guid.NewGuid(), Name = "Test", Description = "",
            OwnerUsername = "alice", IsPublic = true, MemberCount = 1,
            RuleCount = 0, CreatedAt = DateTime.UtcNow
        };
        var vm = new UnifiedChannelViewModel(response);
        vm.IsOwner = true;
        Assert.True(vm.IsOwner);
    }
}
