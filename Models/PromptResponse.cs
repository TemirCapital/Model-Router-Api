namespace ModelRouterApi.Models;

public class PromptResponse
{
    public string Answer { get; set; } = string.Empty;
    public RoutingDecision RoutingDecision { get; set; } = new();
    public ToolExecutionResult? ToolResult { get; set; }
    public long LatencyMs { get; set; }
    public int TokensUsed { get; set; }
    public decimal EstimatedCostUsd { get; set; }
}

public class RoutingDecision
{
    public IntentType IntentType { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public List<string> MatchedSignals { get; set; } = [];
}

public class ToolExecutionResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string RawData { get; set; } = string.Empty;
}
