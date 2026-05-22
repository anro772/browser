using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Models;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

public partial class CopilotSidebarViewModel : ObservableObject, IDisposable
{
    private const string SystemPrompt =
        "You are a helpful browsing assistant embedded in a desktop web browser. " +
        "You have read-only access to the user's current web page — URL, title, visible text, the user's current text selection, and optionally a screenshot when attached. " +
        "Answer questions about the page, summarise it, extract information from it, and give recommendations grounded in what's on screen. " +
        "If the user asks about something the page doesn't cover, say so plainly and answer from general knowledge. " +
        "You cannot click, navigate, type, or change browser settings — if the user asks you to perform an action, explain the limitation and suggest what they can do themselves. " +
        "Format replies in Markdown — use short paragraphs, ## headings for sections, - bullets for lists, and ``` fenced blocks for code so the UI can render them with proper structure.";

    private readonly IOllamaClient _ollamaClient;
    private readonly TabStripViewModel? _tabStrip;
    private readonly IPageContextService? _pageContextService;

    private CancellationTokenSource? _streamCts;
    private BrowserTabItem? _subscribedTab;
    private bool _isDisposed;

    [ObservableProperty]
    private ObservableCollection<ChatMessageItem> _messages = new();

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isOllamaConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisionCapableModel))]
    private string _selectedModel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableModels = new();

    /// True when the selected Ollama model has known vision support. The default
    /// `llama3.2:latest` is text-only — vision lives in `llama3.2-vision`, `llava`,
    /// `bakllava`, `moondream`, `minicpm-v`, `gemma3`, etc.
    public bool IsVisionCapableModel
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedModel)) return false;
            var m = SelectedModel.ToLowerInvariant();
            return m.Contains("vision") || m.Contains("llava") || m.Contains("bakllava")
                || m.Contains("moondream") || m.Contains("minicpm-v") || m.Contains("gemma3")
                || m.Contains("llama4") || m.Contains("qwen2.5-vl") || m.Contains("pixtral");
        }
    }

    [ObservableProperty]
    private string _connectionStatus = "Checking...";

    [ObservableProperty]
    private bool _includePageContext = true;

    [ObservableProperty]
    private bool _attachScreenshotOnNext;

    [ObservableProperty]
    private string _currentPageTitle = string.Empty;

    [ObservableProperty]
    private string _currentPageUrl = string.Empty;

    [ObservableProperty]
    private bool _hasActivePage;

    [ObservableProperty]
    private ObservableCollection<SuggestedPrompt> _suggestedPrompts = new()
    {
        new SuggestedPrompt { Label = "Summarize this page", Glyph = "Document24", PromptText = "Summarize this page in 3 bullet points." },
        new SuggestedPrompt { Label = "Find trackers on this site", Glyph = "Shield24", PromptText = "Are there any privacy concerns or trackers on this page?" },
        new SuggestedPrompt { Label = "Generate a blocking rule", Glyph = "Hash24", PromptText = "Suggest a blocking rule for the trackers on this site." },
        new SuggestedPrompt { Label = "Translate this page", Glyph = "Translate24", PromptText = "Translate the visible text on this page to English." }
    };

    public CopilotSidebarViewModel(IOllamaClient ollamaClient)
        : this(ollamaClient, null, null)
    {
    }

    public CopilotSidebarViewModel(
        IOllamaClient ollamaClient,
        TabStripViewModel? tabStrip,
        IPageContextService? pageContextService)
    {
        _ollamaClient = ollamaClient;
        _tabStrip = tabStrip;
        _pageContextService = pageContextService;

        if (_tabStrip != null)
        {
            _tabStrip.ActiveTabChanged += OnTabStripActiveTabChanged;
            SubscribeToTab(_tabStrip.ActiveTab);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = null;

        if (_tabStrip != null)
        {
            _tabStrip.ActiveTabChanged -= OnTabStripActiveTabChanged;
        }
        UnsubscribeFromTab();
    }

    private void OnTabStripActiveTabChanged(object? sender, BrowserTabItem? tab)
    {
        SubscribeToTab(tab);
    }

    private void SubscribeToTab(BrowserTabItem? tab)
    {
        UnsubscribeFromTab();

        if (tab == null)
        {
            RunOnUIThread(() =>
            {
                CurrentPageTitle = string.Empty;
                CurrentPageUrl = string.Empty;
                HasActivePage = false;
            });
            return;
        }

        _subscribedTab = tab;
        tab.SourceChanged += OnTabSourceChanged;
        tab.TitleChanged += OnTabTitleChanged;

        UpdateCurrentPage(tab.Url, tab.Title);
    }

    private void UnsubscribeFromTab()
    {
        if (_subscribedTab == null) return;
        _subscribedTab.SourceChanged -= OnTabSourceChanged;
        _subscribedTab.TitleChanged -= OnTabTitleChanged;
        _subscribedTab = null;
    }

    private void OnTabSourceChanged(object? sender, string url)
    {
        UpdateCurrentPage(url, _subscribedTab?.Title ?? string.Empty);
    }

    private void OnTabTitleChanged(object? sender, string title)
    {
        UpdateCurrentPage(_subscribedTab?.Url ?? string.Empty, title);
    }

    private void UpdateCurrentPage(string url, string title)
    {
        RunOnUIThread(() =>
        {
            CurrentPageUrl = url ?? string.Empty;
            CurrentPageTitle = string.IsNullOrWhiteSpace(title) ? (url ?? string.Empty) : title;
            HasActivePage = !string.IsNullOrEmpty(url) &&
                (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        });
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
    private async Task UseSuggestedPromptAsync(SuggestedPrompt? prompt)
    {
        if (prompt == null) return;
        UserInput = prompt.PromptText;
        if (SendMessageCommand.CanExecute(null))
        {
            await SendMessageAsync();
        }
    }

    [RelayCommand]
    private void OpenOllamaInstructions()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://ollama.com/download",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Open Ollama instructions error: {ex.Message}");
        }
    }

    private static readonly SuggestedPrompt[] DefaultFollowUps = new[]
    {
        new SuggestedPrompt { Label = "Explain simpler",  Glyph = "LightbulbFilament24", PromptText = "Can you explain that in simpler terms?" },
        new SuggestedPrompt { Label = "Tell me more",     Glyph = "ChatMore24",          PromptText = "Tell me more about this." },
        new SuggestedPrompt { Label = "Key risks",        Glyph = "ShieldError24",       PromptText = "What are the key risks or things to watch out for here?" },
        new SuggestedPrompt { Label = "Next steps",       Glyph = "ArrowForward24",      PromptText = "What should I do next?" }
    };

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

        // Drop follow-up chips from any prior assistant turn — only the latest answer shows them.
        ClearAllFollowUps();

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

        var attachScreenshot = AttachScreenshotOnNext;
        if (attachScreenshot)
        {
            RunOnUIThread(() => AttachScreenshotOnNext = false);
        }

        var assistantMessage = new ChatMessageItem
        {
            Role = "assistant",
            Content = string.Empty,
            Timestamp = DateTime.UtcNow,
            IsStreaming = true
        };
        RunOnUIThread(() => Messages.Add(assistantMessage));

        await RunChatTurnAsync(assistantMessage, attachScreenshot);
    }

    [RelayCommand(CanExecute = nameof(CanRegenerate))]
    public async Task RegenerateLastAnswerAsync()
    {
        if (IsGenerating || Messages.Count == 0) return;

        // Strip the trailing assistant message and replay using the remaining history.
        int lastAssistantIdx = -1;
        for (int i = Messages.Count - 1; i >= 0; i--)
        {
            if (Messages[i].Role == "assistant") { lastAssistantIdx = i; break; }
        }
        if (lastAssistantIdx < 0) return;

        RunOnUIThread(() =>
        {
            Messages.RemoveAt(lastAssistantIdx);
            ClearAllFollowUps();
        });

        var assistantMessage = new ChatMessageItem
        {
            Role = "assistant",
            Content = string.Empty,
            Timestamp = DateTime.UtcNow,
            IsStreaming = true
        };
        RunOnUIThread(() => Messages.Add(assistantMessage));

        await RunChatTurnAsync(assistantMessage, attachScreenshot: false);
    }

    private bool CanRegenerate()
        => !IsGenerating && Messages.Any(m => m.Role == "assistant" && !m.IsStreaming);

    private async Task RunChatTurnAsync(ChatMessageItem assistantMessage, bool attachScreenshot)
    {
        IsGenerating = true;
        RegenerateLastAnswerCommand.NotifyCanExecuteChanged();
        _streamCts = new CancellationTokenSource();

        bool succeeded = false;
        try
        {
            var pageContext = await CapturePageContextAsync(attachScreenshot, _streamCts.Token);
            var chatMessages = BuildChatMessages(pageContext, attachScreenshot);
            var model = string.IsNullOrEmpty(SelectedModel) ? null : SelectedModel;

            await foreach (var token in _ollamaClient.ChatStreamAsync(chatMessages, model, _streamCts.Token))
            {
                RunOnUIThread(() =>
                {
                    assistantMessage.Content += token;
                });
            }
            succeeded = !string.IsNullOrWhiteSpace(assistantMessage.Content);
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
                if (succeeded)
                {
                    foreach (var p in DefaultFollowUps)
                    {
                        assistantMessage.FollowUpPrompts.Add(p);
                    }
                }
            });
            IsGenerating = false;
            RegenerateLastAnswerCommand.NotifyCanExecuteChanged();
            _streamCts?.Dispose();
            _streamCts = null;
        }
    }

    private void ClearAllFollowUps()
    {
        RunOnUIThread(() =>
        {
            foreach (var m in Messages)
            {
                if (m.FollowUpPrompts.Count > 0) m.FollowUpPrompts.Clear();
            }
        });
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

    [RelayCommand]
    public void NewChat()
    {
        // "New chat" and "Clear conversation" both start fresh — keep them as separate
        // commands so the header buttons can be wired independently and labelled clearly.
        RunOnUIThread(() =>
        {
            Messages.Clear();
            UserInput = string.Empty;
            AttachScreenshotOnNext = false;
        });
    }

    [RelayCommand]
    public void SelectModel(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName)) return;
        SelectedModel = modelName;
    }

    [RelayCommand]
    public void ToggleAttachScreenshot()
    {
        AttachScreenshotOnNext = !AttachScreenshotOnNext;
    }

    [RelayCommand]
    public void ToggleIncludePageContext()
    {
        IncludePageContext = !IncludePageContext;
        if (!IncludePageContext)
        {
            // Screenshots without text context make little sense — keep the two in sync.
            AttachScreenshotOnNext = false;
        }
    }

    private async Task<PageContext?> CapturePageContextAsync(bool includeScreenshot, CancellationToken ct)
    {
        if (_tabStrip == null || _pageContextService == null) return null;
        if (!IncludePageContext && !includeScreenshot) return null;

        var tab = _tabStrip.ActiveTab;
        if (tab == null) return null;

        try
        {
            return await _pageContextService.CaptureAsync(tab, includeScreenshot, ct);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Page context capture failed", ex);
            return null;
        }
    }

    public List<OllamaChatMessage> BuildChatMessages(PageContext? pageContext, bool attachScreenshot)
    {
        var chatMessages = new List<OllamaChatMessage>
        {
            new() { Role = "system", Content = SystemPrompt }
        };

        if (IncludePageContext && pageContext != null)
        {
            chatMessages.Add(new OllamaChatMessage
            {
                Role = "system",
                Content = BuildGroundingPrompt(pageContext)
            });
        }

        var history = Messages
            .Where(m => m.Role == "user" || (m.Role == "assistant" && !string.IsNullOrEmpty(m.Content)))
            .ToList();

        for (int i = 0; i < history.Count; i++)
        {
            var msg = history[i];
            var outbound = new OllamaChatMessage
            {
                Role = msg.Role,
                Content = msg.Content
            };

            // Attach screenshot only to the latest user message (the current turn).
            bool isLastUserMessage = msg.Role == "user"
                && !history.Skip(i + 1).Any(m => m.Role == "user");
            if (isLastUserMessage && attachScreenshot && !string.IsNullOrEmpty(pageContext?.ScreenshotBase64))
            {
                outbound.Images = new List<string> { pageContext!.ScreenshotBase64! };
            }

            chatMessages.Add(outbound);
        }

        return chatMessages;
    }

    private static string BuildGroundingPrompt(PageContext ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Current page the user is viewing:");
        sb.Append("URL: ").AppendLine(ctx.Url);
        if (!string.IsNullOrWhiteSpace(ctx.Title))
        {
            sb.Append("Title: ").AppendLine(ctx.Title);
        }
        if (!string.IsNullOrWhiteSpace(ctx.Selection))
        {
            sb.AppendLine("Selected text (focus on this if the question relates to it):");
            sb.Append('"').Append(ctx.Selection).AppendLine("\"");
        }
        if (!string.IsNullOrWhiteSpace(ctx.Text))
        {
            sb.AppendLine("Page content:");
            sb.AppendLine(ctx.Text);
        }
        return sb.ToString();
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

    [ObservableProperty]
    private ObservableCollection<SuggestedPrompt> _followUpPrompts = new();

    [RelayCommand]
    private void CopyContent()
    {
        try
        {
            if (!string.IsNullOrEmpty(Content))
            {
                System.Windows.Clipboard.SetText(Content);
            }
        }
        catch
        {
            // Clipboard access can fail transiently if another app holds the lock; swallow.
        }
    }
}
