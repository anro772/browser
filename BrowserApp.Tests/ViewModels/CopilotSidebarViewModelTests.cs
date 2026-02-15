using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.UI.ViewModels;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

public class CopilotSidebarViewModelTests
{
    private readonly Mock<IOllamaClient> _ollamaClientMock;
    private readonly CopilotSidebarViewModel _viewModel;

    public CopilotSidebarViewModelTests()
    {
        _ollamaClientMock = new Mock<IOllamaClient>();
        _viewModel = new CopilotSidebarViewModel(_ollamaClientMock.Object);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        Assert.Empty(_viewModel.Messages);
        Assert.Equal(string.Empty, _viewModel.UserInput);
        Assert.False(_viewModel.IsGenerating);
        Assert.False(_viewModel.IsOllamaConnected);
        Assert.Equal(string.Empty, _viewModel.SelectedModel);
        Assert.Empty(_viewModel.AvailableModels);
        Assert.Equal("Checking...", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenAvailable_SetsConnectedTrue()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<string> { "llama3.2" });

        await _viewModel.CheckConnectionCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsOllamaConnected);
        Assert.Equal("Connected", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenUnavailable_SetsConnectedFalse()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);

        await _viewModel.CheckConnectionCommand.ExecuteAsync(null);

        Assert.False(_viewModel.IsOllamaConnected);
        Assert.Equal("Disconnected", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenException_SetsDisconnected()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ThrowsAsync(new Exception("fail"));

        await _viewModel.CheckConnectionCommand.ExecuteAsync(null);

        Assert.False(_viewModel.IsOllamaConnected);
        Assert.Equal("Disconnected", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyInput_DoesNothing()
    {
        _viewModel.UserInput = "";

        await _viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public async Task SendMessageAsync_WithWhitespaceInput_DoesNothing()
    {
        _viewModel.UserInput = "   ";

        await _viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public void StopGeneration_DoesNotThrow_WhenNotGenerating()
    {
        var exception = Record.Exception(() => _viewModel.StopGenerationCommand.Execute(null));
        Assert.Null(exception);
    }

    [Fact]
    public void ClearChat_ClearsAllMessages()
    {
        // Add some messages directly
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Hello" });
        _viewModel.Messages.Add(new ChatMessageItem { Role = "assistant", Content = "Hi" });
        Assert.Equal(2, _viewModel.Messages.Count);

        _viewModel.ClearChatCommand.Execute(null);

        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public async Task GenerateRulesForCurrentPageAsync_WithoutService_DoesNothing()
    {
        // ViewModel created with single-param constructor (no rule service)
        await _viewModel.GenerateRulesForCurrentPageCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public async Task ApplyRuleAsync_WithNullJson_DoesNothing()
    {
        await _viewModel.ApplyRuleCommand.ExecuteAsync(null);

        Assert.Empty(_viewModel.Messages);
    }
}

public class CopilotSidebarViewModelWithRuleServiceTests
{
    private readonly Mock<IOllamaClient> _ollamaClientMock;
    private readonly Mock<IRuleGenerationService> _ruleServiceMock;
    private readonly Mock<TabStripViewModel> _tabStripMock;

    public CopilotSidebarViewModelWithRuleServiceTests()
    {
        _ollamaClientMock = new Mock<IOllamaClient>();
        _ruleServiceMock = new Mock<IRuleGenerationService>();
        // TabStripViewModel requires complex setup, so we test rule generation separately
    }

    [Fact]
    public void ChatMessageItem_DefaultValues()
    {
        var item = new ChatMessageItem();

        Assert.Equal("user", item.Role);
        Assert.Equal(string.Empty, item.Content);
        Assert.False(item.IsStreaming);
        Assert.False(item.IsRuleSuggestion);
        Assert.Null(item.SuggestedRuleJson);
    }

    [Fact]
    public void ChatMessageItem_PropertyChangedRaised()
    {
        var item = new ChatMessageItem();
        var changedProps = new List<string>();
        item.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName!);

        item.Content = "Hello";
        item.IsStreaming = true;

        Assert.Contains("Content", changedProps);
        Assert.Contains("IsStreaming", changedProps);
    }

    [Fact]
    public void ChatMessageItem_IsRuleSuggestion_CanBeSet()
    {
        var item = new ChatMessageItem
        {
            Role = "assistant",
            Content = "Suggested rule: Block trackers",
            IsRuleSuggestion = true,
            SuggestedRuleJson = "{\"Name\":\"Block trackers\"}"
        };

        Assert.True(item.IsRuleSuggestion);
        Assert.NotNull(item.SuggestedRuleJson);
    }
}
