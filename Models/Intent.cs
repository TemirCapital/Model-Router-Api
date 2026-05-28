namespace ModelRouterApi.Models;

public enum IntentType
{
    Simple,
    Moderate,
    Analytical,
    Complex,
    Critical,
    Business,
    Embedding,
    Unknown
}

public class Intent
{
    public IntentType Type { get; set; }
    public float Confidence { get; set; }
    public string? Tool { get; set; }
    public List<string> Signals { get; set; } = [];
    public int TokenEstimate { get; set; }
}
