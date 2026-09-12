using System.Text.Json;
using Microsoft.Extensions.Options;

namespace InventoryManagementSystem.Features.AiAssistant;

public sealed record AiAnswer(string Answer, IReadOnlyList<string> ToolsUsed);

public interface IAiInventoryAssistant
{
    Task<AiAnswer> AskAsync(string question, CancellationToken ct = default);
    Task<string> GenerateInsightsAsync(CancellationToken ct = default);
}

public sealed class AiInventoryAssistant : IAiInventoryAssistant
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILlmClient _llm;
    private readonly IInventoryTools _tools;
    private readonly AiOptions _opt;
    private readonly ILogger<AiInventoryAssistant> _log;

    public AiInventoryAssistant(
        ILlmClient llm,
        IInventoryTools tools,
        IOptions<AiOptions> opt,
        ILogger<AiInventoryAssistant> log)
    {
        _llm = llm;
        _tools = tools;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<AiAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        var messages = new List<LlmMessage>
        {
            LlmMessage.System(AiPrompts.SystemPrompt),
            LlmMessage.User(question)
        };

        IReadOnlyList<LlmToolDefinition> definitions;
        try
        {
            definitions = _tools.GetDefinitions();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "GetDefinitions failed");
            return new AiAnswer($"خطأ في تحميل الأدوات: {ex.Message}", Array.Empty<string>());
        }

        var usedTools = new List<string>();

        for (var i = 0; i < _opt.MaxToolIterations; i++)
        {
            LlmCompletion completion;
            try
            {
                completion = await _llm.CompleteAsync(messages, definitions, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LLM call failed");
                return new AiAnswer(
                    $"مقدرتش أوصل للموديل: {ex.Message}",
                    usedTools);
            }

            if (completion.ToolCalls.Count == 0)
                return new AiAnswer(completion.Content ?? "مفيش رد.", usedTools);

            messages.Add(new LlmMessage
            {
                Role = "assistant",
                Content = completion.Content,
                ToolCalls = completion.ToolCalls
            });

            foreach (var call in completion.ToolCalls)
            {
                usedTools.Add(call.Name);
                string resultJson;
                try
                {
                    resultJson = await _tools.InvokeAsync(call.Name, call.ArgumentsJson, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Tool {Tool} failed", call.Name);
                    resultJson = JsonSerializer.Serialize(
                        new { error = ex.Message, tool = call.Name },
                        JsonOpts);
                }
                messages.Add(LlmMessage.Tool(call.Id, resultJson));
            }
        }

        return new AiAnswer("وصلت للحد الأقصى من المحاولات. جرّب سؤال أبسط.", usedTools);
    }

    public async Task<string> GenerateInsightsAsync(CancellationToken ct = default)
    {
        const string prompt =
            "اعملي ملخص تنفيذي قصير عن صحة المخزون. ضيف: " +
            "(1) من 3 لـ 5 مؤشرات، (2) أهم 3 أولويات لإعادة التخزين، (3) خطر أو فرصة. نقط مختصرة.";
        var answer = await AskAsync(prompt, ct);
        return answer.Answer;
    }
}