using System.Text.Json.Serialization;

namespace BobikApp;

public class ChatResponse
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("object")] public string Object { get; set; }

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("choices")] public Choice[] Choices { get; set; }

    [JsonPropertyName("usage")] public Usage Usage { get; set; }
}

public class Choice
{
    [JsonPropertyName("index")] public int Index { get; set; }

    [JsonPropertyName("message")] public Message Message { get; set; }

    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; }

    [JsonPropertyName("logprobs")] public object Logprobs { get; set; }
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}