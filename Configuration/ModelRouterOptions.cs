namespace ModelRouterApi.Configuration;

public class ModelRouterOptions
{
    public const string SectionName = "ModelRouter";

    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();
    public RoutingThresholds RoutingThresholds { get; set; } = new();
    public ModelCostOptions ModelCosts { get; set; } = new();
    public TelemetryOptions Telemetry { get; set; } = new();
}

public class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-01";

    // Named deployments — map to different Azure OpenAI model deployments
    public string FastModelDeployment { get; set; } = "gpt-4o-mini";
    public string ReasoningModelDeployment { get; set; } = "gpt-4o";
    public string EmbeddingModelDeployment { get; set; } = "text-embedding-3-small";
}

public class RoutingThresholds
{
    /// <summary>
    /// Prompts with estimated tokens below this threshold are candidates for the Fast model.
    /// </summary>
    public int SimpleTokenThreshold { get; set; } = 20;

    /// <summary>
    /// Minimum confidence score to trust a classified intent; below this falls back to Reasoning.
    /// </summary>
    public float MinConfidenceThreshold { get; set; } = 0.6f;
}

public class ModelCostOptions
{
    /// <summary>Cost per 1K tokens in USD for the fast model.</summary>
    public decimal FastModelCostPer1KTokens { get; set; } = 0.00015m;

    /// <summary>Cost per 1K tokens in USD for the reasoning model.</summary>
    public decimal ReasoningModelCostPer1KTokens { get; set; } = 0.005m;

    /// <summary>Cost per 1K tokens in USD for the embedding model.</summary>
    public decimal EmbeddingModelCostPer1KTokens { get; set; } = 0.00002m;
}

public class TelemetryOptions
{
    public bool EnableDetailedLogging { get; set; } = true;
    public string? ApplicationInsightsConnectionString { get; set; }
}
