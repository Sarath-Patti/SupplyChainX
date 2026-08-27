namespace SupplyChainX.Application.Common.Configuration;

public class AiOptions
{
    public const string SectionName = "AiCopilot";

    public string Provider { get; set; } = "Auto";
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public string AzureOpenAiEndpoint { get; set; } = string.Empty;
    public string AzureOpenAiApiKey { get; set; } = string.Empty;
    public string AzureOpenAiDeploymentName { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.2;
}
