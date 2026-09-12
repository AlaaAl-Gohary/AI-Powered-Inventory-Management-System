namespace InventoryManagementSystem.Features.AiAssistant;

public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxToolIterations { get; set; } = 5;
    public double Temperature { get; set; } = 0.2;
}
