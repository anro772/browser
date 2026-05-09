using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Services;
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
        Assert.True(_viewModel.IncludePageContext);
        Assert.False(_viewModel.AttachScreenshotOnNext);
        Assert.False(_viewModel.HasActivePage);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenAvailable_SetsConnected()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<string> { "llama3.2" });

        await _viewModel.CheckConnectionAsync();

        Assert.True(_viewModel.IsOllamaConnected);
        Assert.Equal("Connected", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenUnavailable_SetsDisconnected()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);

        await _viewModel.CheckConnectionAsync();

        Assert.False(_viewModel.IsOllamaConnected);
        Assert.Equal("Disconnected", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnectionAsync_OnException_SetsDisconnected()
    {
        _ollamaClientMock.Setup(x => x.IsAvailableAsync()).ThrowsAsync(new Exception("Network error"));

        await _viewModel.CheckConnectionAsync();

        Assert.False(_viewModel.IsOllamaConnected);
        Assert.Equal("Disconnected", _viewModel.ConnectionStatus);
    }

    [Fact]
    public async Task LoadModelsAsync_PopulatesAvailableModels()
    {
        var models = new List<string> { "llama3.2", "mistral", "codellama" };
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(models);

        await _viewModel.LoadModelsAsync();

        Assert.Equal(3, _viewModel.AvailableModels.Count);
        Assert.Equal("llama3.2", _viewModel.SelectedModel);
    }

    [Fact]
    public async Task LoadModelsAsync_EmptyList_DoesNotSetSelectedModel()
    {
        _ollamaClientMock.Setup(x => x.GetModelsAsync()).ReturnsAsync(new List<string>());

        await _viewModel.LoadModelsAsync();

        Assert.Empty(_viewModel.AvailableModels);
        Assert.Equal(string.Empty, _viewModel.SelectedModel);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyInput_DoesNothing()
    {
        _viewModel.UserInput = "";
        await _viewModel.SendMessageAsync();
        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public async Task SendMessageAsync_WhitespaceInput_DoesNothing()
    {
        _viewModel.UserInput = "   ";
        await _viewModel.SendMessageAsync();
        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public void StopGeneration_DoesNotThrow()
    {
        var ex = Record.Exception(() => _viewModel.StopGeneration());
        Assert.Null(ex);
    }

    [Fact]
    public void ClearChat_ClearsMessages()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Hello" });
        _viewModel.Messages.Add(new ChatMessageItem { Role = "assistant", Content = "Hi" });

        _viewModel.ClearChat();

        Assert.Empty(_viewModel.Messages);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var ex = Record.Exception(() => _viewModel.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void MessagesCollection_CanAddItems()
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
    public void BuildChatMessages_WithoutPageContext_HasSystemPromptOnly()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Hello" });

        var result = _viewModel.BuildChatMessages(pageContext: null, attachScreenshot: false);

        Assert.Equal(2, result.Count);
        Assert.Equal("system", result[0].Role);
        Assert.Contains("browsing assistant", result[0].Content);
        Assert.DoesNotContain("privacy settings", result[0].Content);
        Assert.DoesNotContain("content blocking", result[0].Content);
        Assert.Equal("user", result[1].Role);
        Assert.Equal("Hello", result[1].Content);
        Assert.Null(result[1].Images);
    }

    [Fact]
    public void BuildChatMessages_WithPageContext_IncludesGroundingSystemMessage()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Summarise this" });

        var ctx = new PageContext(
            Url: "https://example.com/article",
            Title: "Example Article",
            Selection: null,
            Text: "This is the page body.",
            ScreenshotBase64: null);

        var result = _viewModel.BuildChatMessages(ctx, attachScreenshot: false);

        // base system + grounding system + user
        Assert.Equal(3, result.Count);
        Assert.Equal("system", result[0].Role);
        Assert.Equal("system", result[1].Role);
        Assert.Contains("https://example.com/article", result[1].Content);
        Assert.Contains("Example Article", result[1].Content);
        Assert.Contains("This is the page body.", result[1].Content);
        Assert.Equal("user", result[2].Role);
    }

    [Fact]
    public void BuildChatMessages_WhenIncludePageContextOff_SkipsGrounding()
    {
        _viewModel.IncludePageContext = false;
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Hi" });

        var ctx = new PageContext("https://example.com", "T", null, "body", null);

        var result = _viewModel.BuildChatMessages(ctx, attachScreenshot: false);

        Assert.Equal(2, result.Count);
        Assert.Equal("system", result[0].Role);
        Assert.Equal("user", result[1].Role);
    }

    [Fact]
    public void BuildChatMessages_WithSelection_PromotesSelectionInGrounding()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Explain" });

        var ctx = new PageContext(
            Url: "https://example.com",
            Title: "T",
            Selection: "highlighted sentence",
            Text: "full page body",
            ScreenshotBase64: null);

        var result = _viewModel.BuildChatMessages(ctx, attachScreenshot: false);

        var grounding = result[1].Content;
        Assert.Contains("Selected text", grounding);
        Assert.Contains("highlighted sentence", grounding);
    }

    [Fact]
    public void BuildChatMessages_WithScreenshot_AttachesImagesToLastUserMessage()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "First" });
        _viewModel.Messages.Add(new ChatMessageItem { Role = "assistant", Content = "Answer" });
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Look at this" });

        var ctx = new PageContext("https://example.com", "T", null, "body", ScreenshotBase64: "AAAA");

        var result = _viewModel.BuildChatMessages(ctx, attachScreenshot: true);

        // base + grounding + user + assistant + user(with image)
        var lastUser = result.Last();
        Assert.Equal("user", lastUser.Role);
        Assert.Equal("Look at this", lastUser.Content);
        Assert.NotNull(lastUser.Images);
        Assert.Single(lastUser.Images!);
        Assert.Equal("AAAA", lastUser.Images![0]);

        // Earlier user message must NOT have images attached
        var firstUser = result.First(m => m.Role == "user");
        Assert.Null(firstUser.Images);
    }

    [Fact]
    public void BuildChatMessages_WithScreenshotButFlagOff_DoesNotAttach()
    {
        _viewModel.Messages.Add(new ChatMessageItem { Role = "user", Content = "Hi" });
        var ctx = new PageContext("https://example.com", "T", null, "body", ScreenshotBase64: "AAAA");

        var result = _viewModel.BuildChatMessages(ctx, attachScreenshot: false);

        Assert.Null(result.Last().Images);
    }

    [Fact]
    public void ToggleAttachScreenshot_FlipsFlag()
    {
        Assert.False(_viewModel.AttachScreenshotOnNext);
        _viewModel.ToggleAttachScreenshot();
        Assert.True(_viewModel.AttachScreenshotOnNext);
        _viewModel.ToggleAttachScreenshot();
        Assert.False(_viewModel.AttachScreenshotOnNext);
    }

    [Fact]
    public void ToggleIncludePageContext_WhenTurningOff_ClearsScreenshotFlag()
    {
        _viewModel.AttachScreenshotOnNext = true;
        Assert.True(_viewModel.IncludePageContext);

        _viewModel.ToggleIncludePageContext();

        Assert.False(_viewModel.IncludePageContext);
        Assert.False(_viewModel.AttachScreenshotOnNext);
    }
}

public class ChatMessageItemTests
{
    [Fact]
    public void ChatMessageItem_DefaultValues()
    {
        var item = new ChatMessageItem();

        Assert.Equal("user", item.Role);
        Assert.Equal(string.Empty, item.Content);
        Assert.False(item.IsStreaming);
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
}
