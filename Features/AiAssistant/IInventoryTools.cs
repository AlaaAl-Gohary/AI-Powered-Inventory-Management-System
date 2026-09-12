namespace InventoryManagementSystem.Features.AiAssistant;

public interface IInventoryTools
{
    IReadOnlyList<LlmToolDefinition> GetDefinitions();
    Task<string> InvokeAsync(string name, string argumentsJson, CancellationToken ct = default);
}
