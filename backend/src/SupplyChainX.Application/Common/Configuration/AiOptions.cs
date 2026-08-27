namespace SupplyChainX.Application.Common.Configuration;

public class AiOptions
{
    public const string SectionName = "AiCopilot";

    public string Provider { get; set; } = "Auto"; // "Auto", "AzureOpenAI", "OpenAI", "Local"
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public string AzureOpenAiEndpoint { get; set; } = string.Empty;
    public string AzureOpenAiApiKey { get; set; } = string.Empty;
    public string AzureOpenAiDeploymentName { get; set; } = "gpt-4o-mini";
    public string ApiVersion { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.2;

    public void ValidateProviderConfiguration()
    {
        var provider = Provider.Trim();

        if (provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = !string.IsNullOrWhiteSpace(AzureOpenAiEndpoint)
                ? AzureOpenAiEndpoint
                : Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

            var apiKey = !string.IsNullOrWhiteSpace(AzureOpenAiApiKey)
                ? AzureOpenAiApiKey
                : Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI configuration is incomplete. Both AzureOpenAiEndpoint and AzureOpenAiApiKey (or environment variables AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY) must be provided when Provider is set to 'AzureOpenAI'.");
            }
        }
        else if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = !string.IsNullOrWhiteSpace(OpenAiApiKey)
                ? OpenAiApiKey
                : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI configuration is incomplete. OpenAiApiKey (or environment variable OPENAI_API_KEY) must be provided when Provider is set to 'OpenAI'.");
            }
        }
    }
}
