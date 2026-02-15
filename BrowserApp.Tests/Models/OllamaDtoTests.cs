using System.Text.Json;
using BrowserApp.Core.DTOs.Ollama;
using FluentAssertions;
using Xunit;

namespace BrowserApp.Tests.Models;

public class OllamaDtoTests
{
    // --- OllamaChatRequest ---

    [Fact]
    public void OllamaChatRequest_Serialization_RoundTrips()
    {
        var request = new OllamaChatRequest
        {
            Model = "llama3.2",
            Messages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "system", Content = "You are helpful." },
                new OllamaChatMessage { Role = "user", Content = "Hello!" }
            },
            Stream = true,
            Temperature = 0.7
        };

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<OllamaChatRequest>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Model.Should().Be("llama3.2");
        deserialized.Messages.Should().HaveCount(2);
        deserialized.Messages[0].Role.Should().Be("system");
        deserialized.Messages[0].Content.Should().Be("You are helpful.");
        deserialized.Messages[1].Role.Should().Be("user");
        deserialized.Messages[1].Content.Should().Be("Hello!");
        deserialized.Stream.Should().BeTrue();
        deserialized.Temperature.Should().Be(0.7);
    }

    [Fact]
    public void OllamaChatRequest_DefaultStream_IsFalse()
    {
        var request = new OllamaChatRequest();

        request.Stream.Should().BeFalse();
    }

    [Fact]
    public void OllamaChatRequest_CustomModel_SerializesCorrectly()
    {
        var request = new OllamaChatRequest { Model = "mistral:7b" };

        var json = JsonSerializer.Serialize(request);

        json.Should().Contain("\"model\":\"mistral:7b\"");
    }

    // --- OllamaChatMessage ---

    [Fact]
    public void OllamaChatMessage_Serialization_RoundTrips()
    {
        var message = new OllamaChatMessage
        {
            Role = "assistant",
            Content = "Hello, how can I help?"
        };

        var json = JsonSerializer.Serialize(message);
        var deserialized = JsonSerializer.Deserialize<OllamaChatMessage>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Role.Should().Be("assistant");
        deserialized.Content.Should().Be("Hello, how can I help?");
    }

    [Fact]
    public void OllamaChatMessage_DefaultRole_IsUser()
    {
        var message = new OllamaChatMessage();

        message.Role.Should().Be("user");
    }

    // --- OllamaChatResponse (native /api/chat format) ---

    [Fact]
    public void OllamaChatResponse_Deserialization_ParsesNativeFormat()
    {
        var json = """
        {
            "model": "llama3.2",
            "message": {
                "role": "assistant",
                "content": "Hello!"
            },
            "done": true,
            "total_duration": 5191566416,
            "eval_count": 298
        }
        """;

        var response = JsonSerializer.Deserialize<OllamaChatResponse>(json);

        response.Should().NotBeNull();
        response!.Model.Should().Be("llama3.2");
        response.Done.Should().BeTrue();
        response.Message.Should().NotBeNull();
        response.Message!.Role.Should().Be("assistant");
        response.Message!.Content.Should().Be("Hello!");
        response.TotalDuration.Should().Be(5191566416);
        response.EvalCount.Should().Be(298);
    }

    [Fact]
    public void OllamaChatResponse_Deserialization_HandlesNullMessage()
    {
        var json = """
        {
            "done": true
        }
        """;

        var response = JsonSerializer.Deserialize<OllamaChatResponse>(json);

        response.Should().NotBeNull();
        response!.Model.Should().BeNull();
        response.Message.Should().BeNull();
        response.Done.Should().BeTrue();
    }

    // --- OllamaStreamChunk (native NDJSON format) ---

    [Fact]
    public void OllamaStreamChunk_Deserialization_ParsesNativeFormat()
    {
        var json = """
        {
            "model": "llama3.2",
            "message": {
                "role": "assistant",
                "content": "Hi"
            },
            "done": false
        }
        """;

        var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(json);

        chunk.Should().NotBeNull();
        chunk!.Model.Should().Be("llama3.2");
        chunk.Done.Should().BeFalse();
        chunk.Message.Should().NotBeNull();
        chunk.Message!.Content.Should().Be("Hi");
    }

    [Fact]
    public void OllamaStreamChunk_Deserialization_HandlesDoneChunk()
    {
        var json = """
        {
            "model": "llama3.2",
            "message": {
                "role": "assistant",
                "content": ""
            },
            "done": true
        }
        """;

        var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(json);

        chunk.Should().NotBeNull();
        chunk!.Done.Should().BeTrue();
        chunk.Message.Should().NotBeNull();
        chunk.Message!.Content.Should().BeEmpty();
    }

    [Fact]
    public void OllamaStreamChunk_Deserialization_HandlesNullMessage()
    {
        var json = """
        {
            "done": true
        }
        """;

        var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(json);

        chunk.Should().NotBeNull();
        chunk!.Done.Should().BeTrue();
        chunk.Message.Should().BeNull();
    }

    // --- OllamaModelsResponse ---

    [Fact]
    public void OllamaModelsResponse_Deserialization_ParsesModels()
    {
        var json = """
        {
            "models": [
                {
                    "name": "llama3.2:latest",
                    "model": "llama3.2",
                    "modified_at": "2024-01-15T10:30:00Z",
                    "size": 4000000000
                },
                {
                    "name": "mistral:latest",
                    "model": "mistral",
                    "modified_at": "2024-01-14T08:00:00Z",
                    "size": 3500000000
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<OllamaModelsResponse>(json);

        response.Should().NotBeNull();
        response!.Models.Should().HaveCount(2);
        response.Models[0].Name.Should().Be("llama3.2:latest");
        response.Models[0].Model.Should().Be("llama3.2");
        response.Models[0].Size.Should().Be(4000000000);
        response.Models[1].Name.Should().Be("mistral:latest");
    }

    [Fact]
    public void OllamaModelsResponse_EmptyModels_ParsesEmpty()
    {
        var json = """
        {
            "models": []
        }
        """;

        var response = JsonSerializer.Deserialize<OllamaModelsResponse>(json);

        response.Should().NotBeNull();
        response!.Models.Should().BeEmpty();
    }
}
