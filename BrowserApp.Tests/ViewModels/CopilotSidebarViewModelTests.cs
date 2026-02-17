using System.Runtime.CompilerServices;
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

    private static async IAsyncEnumerable<string> CreateAsyncEnumerable(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    [Fact]
    public async Task SendMessageAsync_WhenGenerating_DoesNotSendAgain()
    {
        _viewModel.IsGenerating = true;
        _viewModel.UserInput = "Hello";
        await _viewModel.SendMessageCommand.ExecuteAsync(null);
        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenConnected_LoadsModels()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<string> { "model1", "model2" });
        await _viewModel.CheckConnectionCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsOllamaConnected);
        Assert.Equal(2, _viewModel.AvailableModels.Count);
    }

    [Fact]
    public async Task LoadModelsAsync_SetsFirstModelAsDefault()
    {
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<string> { "first-model", "second-model" });
        await _viewModel.LoadModelsCommand.ExecuteAsync(null);
        Assert.Equal("first-model", _viewModel.SelectedModel);
    }

    [Fact]
    public async Task LoadModelsAsync_EmptyList_DoesNotSetModel()
    {
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<string>());
        await _viewModel.LoadModelsCommand.ExecuteAsync(null);
        Assert.Equal(string.Empty, _viewModel.SelectedModel);
        Assert.Empty(_viewModel.AvailableModels);
    }

    [Fact]
    public async Task LoadModelsAsync_WhenException_DoesNotCrash()
    {
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ThrowsAsync(new Exception("fail"));
        var ex = await Record.ExceptionAsync(() => _viewModel.LoadModelsCommand.ExecuteAsync(null));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SendMessageAsync_AddsUserAndAssistantMessages()
    {
        _viewModel.AvailableModels.Add("llama3.2");
        _viewModel.SelectedModel = "llama3.2";
        _ollamaClientMock
            .Setup(x => x.ChatStreamAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable("Hello", " world"));
        _viewModel.UserInput = "Test";
        await _viewModel.SendMessageCommand.ExecuteAsync(null);
        Assert.Equal(2, _viewModel.Messages.Count);
        Assert.Equal("user", _viewModel.Messages[0].Role);
        Assert.Equal("Test", _viewModel.Messages[0].Content);
        Assert.Equal("assistant", _viewModel.Messages[1].Role);
        Assert.Contains("Hello", _viewModel.Messages[1].Content);
        Assert.Contains(" world", _viewModel.Messages[1].Content);
    }

    [Fact]
    public async Task SendMessageAsync_SetsIsGeneratingDuringStream()
    {
        _viewModel.AvailableModels.Add("llama3.2");
        _viewModel.SelectedModel = "llama3.2";
        var generatingStates = new List<bool>();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CopilotSidebarViewModel.IsGenerating))
                generatingStates.Add(_viewModel.IsGenerating);
        };
        _ollamaClientMock
            .Setup(x => x.ChatStreamAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable("Hi"));
        _viewModel.UserInput = "Test";
        await _viewModel.SendMessageCommand.ExecuteAsync(null);
        Assert.Contains(true, generatingStates);
        Assert.False(_viewModel.IsGenerating);
    }

    [Fact]
    public void Messages_PreservesOrderOfConversation()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "First" });
        _viewModel.Messages.Add(new ChatMessageItem { Role = "assistant", Content = "Second" });
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Third" });
        Assert.Equal(3, _viewModel.Messages.Count);
        Assert.Equal("First", _viewModel.Messages[0].Content);
        Assert.Equal("Second", _viewModel.Messages[1].Content);
        Assert.Equal("Third", _viewModel.Messages[2].Content);
    }

    [Fact]
    public async Task ApplyRuleAsync_WithEmptyString_DoesNothing()
    {
        await _viewModel.ApplyRuleCommand.ExecuteAsync("");
        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public async Task ApplyRuleAsync_WithInvalidJson_DoesNotCrash()
    {
        // ViewModel created with single-param constructor (no rule service), so it returns early
        var ex = await Record.ExceptionAsync(() => _viewModel.ApplyRuleCommand.ExecuteAsync("not json"));
        Assert.Null(ex);
    }
}

public class CopilotSidebarViewModelWithRuleServiceTests
{
    private readonly Mock<IOllamaClient> _ollamaClientMock;
    private readonly Mock<IRuleGenerationService> _ruleServiceMock;

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
