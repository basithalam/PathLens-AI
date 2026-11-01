namespace PathLens.Infrastructure.Ai;

/// <summary>
/// Meta AI configuration. The API key comes from server-side configuration
/// (environment variable MetaAi__ApiKey or user secrets), never from the
/// frontend and never committed.
/// </summary>
public sealed class MetaAiOptions
{
    public const string SectionName = "MetaAi";

    /// <summary>OpenAI-compatible chat completions endpoint. Examples: https://api.llama.com/v1/chat/completions (Meta Llama API) or https://api.groq.com/openai/v1/chat/completions (Groq). G1 fix: old https://api.meta.ai/compat/v1 returned 404.</summary>
    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public int TimeoutSeconds { get; set; } = 60;
}
