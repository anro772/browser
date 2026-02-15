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

    // --- OllamaChatResponse ---

    [Fact]
    public void OllamaChatResponse_Deserialization_ParsesChoices()
    {
        var json = """
        {
            "id": "chatcmpl-123",
            "object": "chat.completion",
            "created": 1700000000,
            "model": "llama3.2",
            "choices": [
                {
                    "index": 0,
                    "message": {
                        "role": "assistant",
                        "content": "Hello!"
                    },
                    "finish_reason": "stop"
                }
            ],
            "usage": {
                "prompt_tokens": 10,
                "completion_tokens": 5,
                "total_tokens": 15
            }
        }
        """;

        var response = JsonSerializer.Deserialize<OllamaChatResponse>(json);

        response.Should().NotBeNull();
        response!.Id.Should().Be("chatcmpl-123");
        response.Model.Should().Be("llama3.2");
        response.Choices.Should().HaveCount(1);
        response.Choices[0].Index.Should().Be(0);
        response.Choices[0].Message.Should().NotBeNull();
        response.Choices[0].Message!.Role.Should().Be("assistant");
        response.Choices[0].Message!.Content.Should().Be("Hello!");
        response.Choices[0].FinishReason.Should().Be("stop");
        response.Usage.Should().NotBeNull();
        response.Usage!.PromptTokens.Should().Be(10);
        response.Usage.CompletionTokens.Should().Be(5);
        response.Usage.TotalTokens.Should().Be(15);
    }

    [Fact]
    public void OllamaChatResponse_Deserialization_HandlesNullFields()
    {
        var json = """
        {
            "choices": []
        }
        """;

        var response = JsonSerializer.Deserialize<OllamaChatResponse>(json);

        response.Should().NotBeNull();
        response!.Id.Should().BeNull();
        response.Model.Should().BeNull();
        response.Usage.Should().BeNull();
        response.Choices.Should().BeEmpty();
    }

    // --- OllamaStreamChunk ---

    [Fact]
    public void OllamaStreamChunk_Deserialization_ParsesDelta()
    {
        var json = """
        {
            "id": "chatcmpl-456",
            "object": "chat.completion.chunk",
            "created": 1700000001,
            "model": "llama3.2",
            "choices": [
                {
                    "index": 0,
                    "delta": {
                        "role": "assistant",
                        "content": "Hi"
                    },
                    "finish_reason": null
                }
            ]
        }
        """;

        var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(json);

        chunk.Should().NotBeNull();
        chunk!.Id.Should().Be("chatcmpl-456");
        chunk.Choices.Should().HaveCount(1);
        chunk.Choices[0].Delta.Should().NotBeNull();
        chunk.Choices[0].Delta!.Role.Should().Be("assistant");
        chunk.Choices[0].Delta!.Content.Should().Be("Hi");
        chunk.Choices[0].FinishReason.Should().BeNull();
    }

    [Fact]
    public void OllamaStreamChunk_Deserialization_HandlesNullDelta()
    {
        var json = """
        {
            "choices": [
                {
                    "index": 0,
                    "delta": null,
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(json);

        chunk.Should().NotBeNull();
        chunk!.Choices.Should().HaveCount(1);
        chunk.Choices[0].Delta.Should().BeNull();
        chunk.Choices[0].FinishReason.Should().Be("stop");
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

    // --- OllamaUsage ---

    [Fact]
    public void OllamaUsage_Deserialization_ParsesTokenCounts()
    {
        var json = """
        {
            "prompt_tokens": 25,
            "completion_tokens": 50,
            "total_tokens": 75
        }
        """;

        var usage = JsonSerializer.Deserialize<OllamaUsage>(json);

        usage.Should().NotBeNull();
        usage!.PromptTokens.Should().Be(25);
        usage.CompletionTokens.Should().Be(50);
        usage.TotalTokens.Should().Be(75);
    }
}
