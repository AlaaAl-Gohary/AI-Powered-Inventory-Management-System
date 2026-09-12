using InventoryManagementSystem.Features.AiAssistant;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

public sealed class AiAssistantController : Controller
{
    private readonly IAiInventoryAssistant _assistant;
    private readonly ILogger<AiAssistantController> _log;

    public AiAssistantController(IAiInventoryAssistant assistant, ILogger<AiAssistantController> log)
    {
        _assistant = assistant;
        _log = log;
    }

    public IActionResult Index() => View();

    // ===== Endpoint تشخيصي — مايستخدمش الـ AI =====
    [HttpGet]
    public async Task<IActionResult> Diag(CancellationToken ct)
    {
        var report = new Dictionary<string, object?>();
        try
        {
            var tools = HttpContext.RequestServices.GetRequiredService<IInventoryTools>();
            var json = await tools.InvokeAsync("get_inventory_summary", "{}", ct);
            report["database"] = "OK";
            report["summary"] = json;
        }
        catch (Exception ex)
        {
            report["database"] = "FAIL: " + ex.Message;
            report["databaseStackTrace"] = ex.StackTrace;
        }
        report["apiKeySet"] = !string.IsNullOrWhiteSpace(
            HttpContext.RequestServices
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>()
                .Value.ApiKey);
        return Json(report);
    }

    // ===== Endpoint الأسئلة =====
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ask([FromBody] AskRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Question))
            return Json(new { answer = "من فضلك اكتب سؤال.", toolsUsed = Array.Empty<string>() });

        try
        {
            var result = await _assistant.AskAsync(req.Question, ct);
            return Json(new { answer = result.Answer, toolsUsed = result.ToolsUsed });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AI Assistant error");
            return Json(new
            {
                answer = $"خطأ: {ex.GetType().Name} — {ex.Message}",
                toolsUsed = Array.Empty<string>(),
                details = ex.StackTrace
            });
        }
    }

    // ===== Endpoint الملخص التنفيذي =====
    [HttpGet]
    public async Task<IActionResult> Insights(CancellationToken ct)
    {
        try
        {
            var text = await _assistant.GenerateInsightsAsync(ct);
            return Json(new { insight = text });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AI Insights error");
            return Json(new { insight = $"خطأ: {ex.GetType().Name} — {ex.Message}" });
        }
    }

    public sealed record AskRequest(string Question);
}