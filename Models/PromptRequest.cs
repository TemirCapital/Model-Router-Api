namespace ModelRouterApi.Models;

public class PromptRequest
{
    public string Text { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public RoutingHint? RoutingHint { get; set; }
}

/// <summary>
/// Optional caller-supplied hint to override or bias routing decisions.
/// Useful for Copilot Studio / Foundry integrations passing context signals.
/// </summary>
public class RoutingHint
{
    public bool ForceReasoning { get; set; }
    public bool ForceFast { get; set; }
    public string? PreferredModel { get; set; }
}
