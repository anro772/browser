using System.Net.Http;
using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Services;
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
    private readonly SettingsService _settingsService = new();

    private ChannelsViewModel CreateVm() =>
        new(_apiClientMock.Object, _syncServiceMock.Object, _scopeFactoryMock.Object, _ruleEngineMock.Object, _settingsService);

    [Fact]
    public void InitialState_DefaultUsername()
    {
        var vm = CreateVm();
        // Username comes from SettingsService.ApiUsername which defaults to "default_user" when empty
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

/// <summary>
/// Tests for the QOL additions to ChannelsViewModel:
/// extracted LoadChannelRulesAsync helper, TotalChannels property, ShowChannelDetailsCommand.
/// </summary>
public class ChannelsViewModelQolTests
{
    private readonly Mock<IChannelApiClient> _api = new();
    private readonly Mock<IChannelSyncService> _sync = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IRuleEngine> _ruleEngine = new();
    private readonly SettingsService _settings = new();

    private ChannelsViewModel CreateVm() =>
        new(_api.Object, _sync.Object, _scopeFactory.Object, _ruleEngine.Object, _settings);

    private static UnifiedChannelViewModel MakeChannel(bool joined, Guid? id = null)
    {
        var resp = new ChannelResponse
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test",
            Description = "",
            OwnerUsername = "alice",
            IsPublic = true,
            MemberCount = 1,
            RuleCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        if (!joined) return new UnifiedChannelViewModel(resp);
        var ms = new ChannelMembershipDto
        {
            Id = Guid.NewGuid().ToString(),
            ChannelId = resp.Id.ToString(),
            ChannelName = resp.Name,
            ChannelDescription = resp.Description,
            Username = "bob",
            IsActive = true,
            JoinedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow,
            RuleCount = 0
        };
        return new UnifiedChannelViewModel(resp, ms);
    }

    [Fact]
    public void TotalChannels_StartsAtZero()
    {
        Assert.Equal(0, CreateVm().TotalChannels);
    }

    [Fact]
    public async Task LoadChannelRulesAsync_PopulatesRulePreview()
    {
        var vm = CreateVm();
        var channel = MakeChannel(joined: true);
        var ruleId = Guid.NewGuid();

        _api.Setup(a => a.GetChannelRulesAsync(channel.Id, It.IsAny<string>()))
            .ReturnsAsync(new ChannelRuleListResponse
            {
                Rules = new List<ChannelRuleResponse>
                {
                    new() { Id = ruleId, ChannelId = channel.Id, Name = "Test Rule", Site = "*", IsEnforced = true, RulesJson = "[]" }
                }
            });

        await vm.LoadChannelRulesAsync(channel);

        Assert.Single(channel.RulePreview);
        Assert.Equal(ruleId, channel.RulePreview[0].Id);
        Assert.Equal("Test Rule", channel.RulePreview[0].Name);
        Assert.True(channel.RulePreview[0].IsEnforced);
    }

    [Fact]
    public async Task LoadChannelRulesAsync_TogglesLoadingFlag()
    {
        var vm = CreateVm();
        var channel = MakeChannel(joined: true);

        _api.Setup(a => a.GetChannelRulesAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ChannelRuleListResponse { Rules = new() });

        await vm.LoadChannelRulesAsync(channel);

        // IsLoadingPreview ends false regardless of success
        Assert.False(channel.IsLoadingPreview);
    }

    [Fact]
    public async Task LoadChannelRulesAsync_ApiFailure_DoesNotThrow()
    {
        var vm = CreateVm();
        var channel = MakeChannel(joined: true);

        _api.Setup(a => a.GetChannelRulesAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        // Should swallow internally so the dialog can still render with an empty list.
        await vm.LoadChannelRulesAsync(channel);
        Assert.False(channel.IsLoadingPreview);
        Assert.Empty(channel.RulePreview);
    }

    [Fact]
    public void ShowChannelDetailsCommand_Exists()
    {
        // The dialog itself requires an STA host; we just verify the command is wired.
        var vm = CreateVm();
        Assert.NotNull(vm.ShowChannelDetailsCommand);
    }

    [Fact]
    public void ShowChannelDetailsCommand_NullChannel_NoOp()
    {
        var vm = CreateVm();
        // Won't open a dialog when channel is null.
        var task = vm.ShowChannelDetailsCommand.ExecuteAsync(null);
        Assert.True(task.IsCompleted);
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
