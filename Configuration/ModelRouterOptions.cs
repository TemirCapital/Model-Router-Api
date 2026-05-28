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
    public string ApiVersion { get; set; } = "2025-01-01-preview";

    // ── 5-tier model deployments ─────────────────────────────────────────
    /// <summary>Tier 1 - cheapest/fastest. e.g. gpt-4o-mini</summary>
    public string FastModelDeployment { get; set; } = "gpt-4o-mini";

    /// <summary>Tier 2 - moderate complexity. e.g. gpt-4.1-mini</summary>
    public string ModerateModelDeployment { get; set; } = "gpt-4.1-mini";

    /// <summary>Tier 3 - analytical reasoning. e.g. gpt-4o</summary>
    public string ReasoningModelDeployment { get; set; } = "gpt-4o";

    /// <summary>Tier 4 - deep complex tasks. e.g. gpt-4.1</summary>
    public string ComplexModelDeployment { get; set; } = "gpt-4.1";

    /// <summary>Tier 5 - chain-of-thought / critical incidents. e.g. o4-mini</summary>
    public string CriticalModelDeployment { get; set; } = "o4-mini";

    /// <summary>Embeddings. e.g. text-embedding-3-small</summary>
    public string EmbeddingModelDeployment { get; set; } = "text-embedding-3-small";
}

public class RoutingThresholds
{
    /// <summary>Prompts with estimated tokens below this -> Fast model candidate.</summary>
    public int SimpleTokenThreshold { get; set; } = 20;

    /// <summary>Prompts with estimated tokens below this -> Moderate model candidate.</summary>
    public int ModerateTokenThreshold { get; set; } = 60;

    /// <summary>Prompts with estimated tokens below this -> Analytical model candidate.</summary>
    public int AnalyticalTokenThreshold { get; set; } = 150;

    /// <summary>Minimum confidence to trust classification; below this falls back to Analytical.</summary>
    public float MinConfidenceThreshold { get; set; } = 0.6f;
}

public class ModelCostOptions
{
    /// <summary>Cost per 1K tokens in USD for the fast model (gpt-4o-mini).</summary>
    public decimal FastModelCostPer1KTokens { get; set; } = 0.00015m;

    /// <summary>Cost per 1K tokens in USD for the moderate model (gpt-4.1-mini).</summary>
    public decimal ModerateModelCostPer1KTokens { get; set; } = 0.0004m;

    /// <summary>Cost per 1K tokens in USD for the reasoning model (gpt-4o).</summary>
    public decimal ReasoningModelCostPer1KTokens { get; set; } = 0.005m;

    /// <summary>Cost per 1K tokens in USD for the complex model (gpt-4.1).</summary>
    public decimal ComplexModelCostPer1KTokens { get; set; } = 0.008m;

    /// <summary>Cost per 1K tokens in USD for the critical model (o4-mini).</summary>
    public decimal CriticalModelCostPer1KTokens { get; set; } = 0.011m;

    /// <summary>Cost per 1K tokens in USD for the embedding model.</summary>
    public decimal EmbeddingModelCostPer1KTokens { get; set; } = 0.00002m;
}

public class TelemetryOptions
{
    public bool EnableDetailedLogging { get; set; } = true;
    public string? ApplicationInsightsConnectionString { get; set; }
}
