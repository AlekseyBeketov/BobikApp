using System.Text.Json.Serialization;
namespace BobikApp;

public class OllamaResponse
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("created_at")] public string CreatedAt { get; set; }

    [JsonPropertyName("message")] public OllamaMessage Message { get; set; }

    [JsonPropertyName("done_reason")] public string DoneReason { get; set; }

    [JsonPropertyName("done")] public bool Done { get; set; }

    [JsonPropertyName("total_duration")] public long TotalDuration { get; set; }

    [JsonPropertyName("load_duration")] public long LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("prompt_eval_duration")]
    public long PromptEvalDuration { get; set; }

    [JsonPropertyName("eval_count")] public int EvalCount { get; set; }

    [JsonPropertyName("eval_duration")] public long EvalDuration { get; set; }
}