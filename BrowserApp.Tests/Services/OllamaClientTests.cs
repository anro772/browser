using System.Net;
using System.Text;
using System.Text.Json;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.UI.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace BrowserApp.Tests.Services;

public class OllamaClientTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly OllamaClient _client;

    public OllamaClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        _client = new OllamaClient(_httpClient, "llama3.2");
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServerResponds_ReturnsTrue()
    {
        SetupHandler(HttpStatusCode.OK, "{\"models\":[]}");

        var result = await _client.IsAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServerDown_ReturnsFalse()
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _client.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServerReturns500_ReturnsFalse()
    {
        SetupHandler(HttpStatusCode.InternalServerError, "error");

        var result = await _client.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModelNames()
    {
        var response = new OllamaModelsResponse
        {
            Models = new List<OllamaModel>
            {
                new() { Name = "llama3.2", Model = "llama3.2" },
                new() { Name = "mistral", Model = "mistral" }
            }
        };
        SetupHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        var models = await _client.GetModelsAsync();

        Assert.Equal(2, models.Count);
        Assert.Contains("llama3.2", models);
        Assert.Contains("mistral", models);
    }

    [Fact]
    public async Task GetModelsAsync_WhenServerDown_ReturnsEmptyList()
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var models = await _client.GetModelsAsync();

        Assert.Empty(models);
    }

    [Fact]
    public async Task ChatAsync_ReturnsAssistantContent()
    {
        var chatResponse = new OllamaChatResponse
        {
            Message = new OllamaChatMessage { Role = "assistant", Content = "Hello! How can I help?" },
            Done = true
        };
        SetupHandler(HttpStatusCode.OK, JsonSerializer.Serialize(chatResponse));

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "user", Content = "Hello" }
        };

        var result = await _client.ChatAsync(messages);

        Assert.Equal("Hello! How can I help?", result);
    }

    [Fact]
    public async Task ChatAsync_WithNullMessage_ReturnsEmptyString()
    {
        var chatResponse = new OllamaChatResponse { Message = null, Done = true };
        SetupHandler(HttpStatusCode.OK, JsonSerializer.Serialize(chatResponse));

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "user", Content = "Hello" }
        };

        var result = await _client.ChatAsync(messages);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ChatAsync_SendsCorrectRequestFormat()
    {
        var chatResponse = new OllamaChatResponse
        {
            Message = new OllamaChatMessage { Content = "ok" },
            Done = true
        };

        HttpRequestMessage? capturedRequest = null;
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(chatResponse), Encoding.UTF8, "application/json")
            });

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "system", Content = "You are helpful" },
            new() { Role = "user", Content = "Test" }
        };

        await _client.ChatAsync(messages, "custom-model");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/api/chat", capturedRequest.RequestUri?.AbsolutePath);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<OllamaChatRequest>(body);
        Assert.Equal("custom-model", request?.Model);
        Assert.Equal(2, request?.Messages.Count);
        Assert.False(request?.Stream);
    }

    [Fact]
    public async Task ChatAsync_UsesDefaultModel_WhenModelIsNull()
    {
        var chatResponse = new OllamaChatResponse
        {
            Message = new OllamaChatMessage { Content = "ok" },
            Done = true
        };

        HttpRequestMessage? capturedRequest = null;
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(chatResponse), Encoding.UTF8, "application/json")
            });

        await _client.ChatAsync(new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } });

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<OllamaChatRequest>(body);
        Assert.Equal("llama3.2", request?.Model);
    }

    [Fact]
    public async Task ChatStreamAsync_YieldsContentDeltas()
    {
        // Ollama native streaming: NDJSON (one JSON object per line)
        var ndjson = "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\" world\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}\n";

        SetupStreamHandler(ndjson);

        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "user", Content = "Hello" }
        };

        var tokens = new List<string>();
        await foreach (var token in _client.ChatStreamAsync(messages))
        {
            tokens.Add(token);
        }

        Assert.Equal(2, tokens.Count);
        Assert.Equal("Hello", tokens[0]);
        Assert.Equal(" world", tokens[1]);
    }

    [Fact]
    public async Task ChatStreamAsync_HandlesEmptyContent()
    {
        var ndjson = "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"Hi\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}\n";

        SetupStreamHandler(ndjson);

        var tokens = new List<string>();
        await foreach (var token in _client.ChatStreamAsync(new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } }))
        {
            tokens.Add(token);
        }

        Assert.Single(tokens);
        Assert.Equal("Hi", tokens[0]);
    }

    [Fact]
    public async Task ChatStreamAsync_SupportsCancellation()
    {
        var ndjson = "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\" world\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}\n";

        SetupStreamHandler(ndjson);

        var cts = new CancellationTokenSource();
        var tokens = new List<string>();

        await foreach (var token in _client.ChatStreamAsync(
            new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } },
            cancellationToken: cts.Token))
        {
            tokens.Add(token);
            cts.Cancel(); // Cancel after first token
        }

        Assert.Single(tokens);
    }

    [Fact]
    public async Task ChatAsync_WhenServerReturns400_ThrowsHttpRequestException()
    {
        SetupHandler(HttpStatusCode.BadRequest, "Bad request");
        var messages = new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } };
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.ChatAsync(messages));
    }

    [Fact]
    public async Task ChatAsync_WhenResponseBodyIsEmpty_ReturnsEmptyString()
    {
        var chatResponse = new OllamaChatResponse { Message = null, Done = true };
        SetupHandler(HttpStatusCode.OK, JsonSerializer.Serialize(chatResponse));
        var messages = new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } };
        var result = await _client.ChatAsync(messages);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ChatAsync_WhenResponseIsInvalidJson_ThrowsException()
    {
        SetupHandler(HttpStatusCode.OK, "not valid json at all {{{");
        var messages = new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } };
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => _client.ChatAsync(messages));
    }

    [Fact]
    public async Task ChatStreamAsync_WithDoneOnly_YieldsNothing()
    {
        var ndjson = "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}\n";
        SetupStreamHandler(ndjson);

        var tokens = new List<string>();
        await foreach (var token in _client.ChatStreamAsync(new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } }))
            tokens.Add(token);
        Assert.Empty(tokens);
    }

    [Fact]
    public async Task ChatStreamAsync_WithMalformedJson_SkipsInvalidChunks()
    {
        var ndjson = "not-valid-json\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"OK\"},\"done\":false}\n" +
                     "{\"model\":\"llama3.2\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}\n";
        SetupStreamHandler(ndjson);

        var tokens = new List<string>();
        await foreach (var token in _client.ChatStreamAsync(new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } }))
            tokens.Add(token);
        Assert.Single(tokens);
        Assert.Equal("OK", tokens[0]);
    }

    [Fact]
    public async Task ChatStreamAsync_ServerError_ThrowsException()
    {
        SetupHandler(HttpStatusCode.InternalServerError, "Server error");
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in _client.ChatStreamAsync(new List<OllamaChatMessage> { new() { Role = "user", Content = "Hi" } }))
            { }
        });
    }

    [Fact]
    public async Task GetModelsAsync_EmptyModelsList_ReturnsEmptyList()
    {
        var response = new OllamaModelsResponse { Models = new List<OllamaModel>() };
        SetupHandler(HttpStatusCode.OK, JsonSerializer.Serialize(response));
        var models = await _client.GetModelsAsync();
        Assert.Empty(models);
    }

    private void SetupHandler(HttpStatusCode statusCode, string content)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private void SetupStreamHandler(string ndjsonContent)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ndjsonContent, Encoding.UTF8, "application/x-ndjson")
            });
    }
}
