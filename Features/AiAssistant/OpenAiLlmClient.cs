using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InventoryManagementSystem.Features.AiAssistant;

public sealed class OpenAiLlmClient : ILlmClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _http;
    private readonly AiOptions _opt;

    public OpenAiLlmClient(HttpClient http, IOptions<AiOptions> opt)
    {
        _http = http;
        _opt = opt.Value;
        _http.BaseAddress ??= new Uri(_opt.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _opt.ApiKey);
    }

    public async Task<LlmCompletion> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = _opt.Model,
            ["temperature"] = _opt.Temperature,
            ["messages"] = messages.Select(ToWire).ToArray()
        };

        if (tools.Count > 0)
        {
            payload["tools"] = tools.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.ParametersSchema
                }
            }).ToArray();
            payload["tool_choice"] = "auto";
        }

        var bodyJson = JsonSerializer.Serialize(payload, Json);

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
        };

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"LLM call failed ({(int)resp.StatusCode}) on model '{_opt.Model}': {body}");

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidOperationException($"LLM response had no choices: {body}");

        var msg = choices[0].GetProperty("message");

        var result = new LlmCompletion();
        if (msg.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
            result.Content = c.GetString();

        if (msg.TryGetProperty("tool_calls", out var tcs) && tcs.ValueKind == JsonValueKind.Array)
        {
            foreach (var tc in tcs.EnumerateArray())
            {
                var fn = tc.GetProperty("function");
                result.ToolCalls.Add(new LlmToolCall
                {
                    Id = tc.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString("N")
                                                              : Guid.NewGuid().ToString("N"),
                    Name = fn.GetProperty("name").GetString() ?? "",
                    ArgumentsJson = fn.TryGetProperty("arguments", out var a) ? a.GetString() ?? "{}" : "{}"
                });
            }
        }
        return result;
    }

    private static object ToWire(LlmMessage m)
    {
        if (m.Role == "tool")
            return new { role = "tool", tool_call_id = m.ToolCallId, content = m.Content ?? "" };

        if (m.Role == "assistant" && m.ToolCalls is { Count: > 0 })
            return new
            {
                role = "assistant",
                content = m.Content,
                tool_calls = m.ToolCalls.Select(t => new
                {
                    id = t.Id,
                    type = "function",
                    function = new { name = t.Name, arguments = t.ArgumentsJson }
                }).ToArray()
            };

        return new { role = m.Role, content = m.Content ?? "" };
    }
}

public sealed class StubLlmClient : ILlmClient
{
    public Task<LlmCompletion> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        CancellationToken ct = default)
        => Task.FromResult(new LlmCompletion
        {
            Content = "الـ Inventory Copilot مش متظبط. ضيف Ai:ApiKey في user-secrets."
        });
}