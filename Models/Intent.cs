namespace ModelRouterApi.Models;

public enum IntentType
{
    Simple,
    Analytical,
    Business,
    Embedding,
    Unknown
}

public class Intent
{
    public IntentType Type { get; set; }
    public float Confidence { get; set; }
    public string? Tool { get; set; }           // tool name to invoke for Business intents
    public List<string> Signals { get; set; } = []; // keywords / features that matched
    public int TokenEstimate { get; set; }
}
