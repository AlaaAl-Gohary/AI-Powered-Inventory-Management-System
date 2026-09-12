namespace InventoryManagementSystem.Features.AiAssistant;

public sealed class LlmMessage
{
    public string Role { get; set; } = "user";
    public string? Content { get; set; }
    public string? ToolCallId { get; set; }
    public List<LlmToolCall>? ToolCalls { get; set; }

    public static LlmMessage System(string c) => new() { Role = "system", Content = c };
    public static LlmMessage User(string c)   => new() { Role = "user",   Content = c };
    public static LlmMessage Tool(string id, string c) => new() { Role = "tool", ToolCallId = id, Content = c };
}

public sealed class LlmToolCall
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ArgumentsJson { get; set; } = "{}";
}

public sealed class LlmToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object ParametersSchema { get; set; } = new { type = "object", properties = new { } };
}

public sealed class LlmCompletion
{
    public string? Content { get; set; }
    public List<LlmToolCall> ToolCalls { get; set; } = new();
}

public interface ILlmClient
{
    Task<LlmCompletion> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken ct = default);
}
