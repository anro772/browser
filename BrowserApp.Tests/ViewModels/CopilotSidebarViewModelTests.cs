using System.Runtime.CompilerServices;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;
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
