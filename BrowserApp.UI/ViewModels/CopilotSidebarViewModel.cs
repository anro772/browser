using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.ViewModels;

public partial class CopilotSidebarViewModel : ObservableObject, IDisposable
{
    private readonly IOllamaClient _ollamaClient;
    private CancellationTokenSource? _streamCts;

    [ObservableProperty]
    private ObservableCollection<ChatMessageItem> _messages = new();

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isOllamaConnected;

    [ObservableProperty]
    private string _selectedModel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableModels = new();

    [ObservableProperty]
    private string _connectionStatus = "Checking...";

    public CopilotSidebarViewModel(IOllamaClient ollamaClient)
    {
        _ollamaClient = ollamaClient;
    }

    public void Dispose()
    {
        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = null;
    }

    private static void RunOnUIThread(Action action)
    {
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    [RelayCommand]
    public async Task CheckConnectionAsync()
    {
        try
        {
            ConnectionStatus = "Checking...";
            IsOllamaConnected = await _ollamaClient.IsAvailableAsync();
            ConnectionStatus = IsOllamaConnected ? "Connected" : "Disconnected";

            if (IsOllamaConnected)
            {
                await LoadModelsAsync();
                if (AvailableModels.Count == 0)
                {
                    ConnectionStatus = "No models installed";
                }
            }
        }
        catch
        {
            IsOllamaConnected = false;
            ConnectionStatus = "Disconnected";
        }
    }

    [RelayCommand]
    public async Task LoadModelsAsync()
    {
        try
        {
            var models = await _ollamaClient.GetModelsAsync();

            RunOnUIThread(() =>
            {
                AvailableModels.Clear();
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }

                if (AvailableModels.Count > 0 && string.IsNullOrEmpty(SelectedModel))
                {
                    SelectedModel = AvailableModels[0];
                }
            });
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to load Ollama models", ex);
        }
    }

    [RelayCommand]
    public async Task SendMessageAsync()
    {
        var input = UserInput?.Trim();
        if (string.IsNullOrEmpty(input) || IsGenerating)
            return;

        if (AvailableModels.Count == 0)
        {
            RunOnUIThread(() =>
            {
                Messages.Add(new ChatMessageItem
                {
                    Role = "assistant",
                    Content = "No models installed. Run 'ollama pull llama3.2' in your terminal to download a model.",
                    Timestamp = DateTime.UtcNow
                });
                UserInput = string.Empty;
            });
            return;
        }

        // Add user message
        var userMessage = new ChatMessageItem
        {
            Role = "user",
            Content = input,
            Timestamp = DateTime.UtcNow
        };

        RunOnUIThread(() =>
        {
            Messages.Add(userMessage);
            UserInput = string.Empty;
        });

        // Add placeholder assistant message for streaming
        var assistantMessage = new ChatMessageItem
        {
            Role = "assistant",
            Content = string.Empty,
            Timestamp = DateTime.UtcNow,
            IsStreaming = true
        };

        RunOnUIThread(() => Messages.Add(assistantMessage));

        IsGenerating = true;
        _streamCts = new CancellationTokenSource();

        try
        {
            var chatMessages = BuildChatMessages();
            var model = string.IsNullOrEmpty(SelectedModel) ? null : SelectedModel;

            await foreach (var token in _ollamaClient.ChatStreamAsync(chatMessages, model, _streamCts.Token))
            {
                RunOnUIThread(() =>
                {
                    assistantMessage.Content += token;
                });
            }
        }
        catch (OperationCanceledException)
        {
            RunOnUIThread(() =>
            {
                assistantMessage.Content += "\n[Generation stopped]";
            });
        }
        catch (Exception ex)
        {
            RunOnUIThread(() =>
            {
                assistantMessage.Content = $"Error: {ex.Message}";
            });
            ErrorLogger.LogError("Chat stream error", ex);
        }
        finally
        {
            RunOnUIThread(() =>
            {
                assistantMessage.IsStreaming = false;
            });
            IsGenerating = false;
            _streamCts?.Dispose();
            _streamCts = null;
        }
    }

    [RelayCommand]
    public void StopGeneration()
    {
        _streamCts?.Cancel();
    }

    [RelayCommand]
    public void ClearChat()
    {
        RunOnUIThread(() => Messages.Clear());
    }

    private List<OllamaChatMessage> BuildChatMessages()
    {
        var chatMessages = new List<OllamaChatMessage>
        {
            new()
            {
                Role = "system",
                Content = "You are a helpful browser assistant. You can help with web browsing, privacy settings, and content blocking rules."
            }
        };

        foreach (var msg in Messages)
        {
            if (msg.Role == "user" || (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.Content)))
            {
                chatMessages.Add(new OllamaChatMessage
                {
                    Role = msg.Role,
                    Content = msg.Content
                });
            }
        }

        return chatMessages;
    }
}

public partial class ChatMessageItem : ObservableObject
{
    [ObservableProperty]
    private string _role = "user";

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.UtcNow;

    [ObservableProperty]
    private bool _isStreaming;
}
