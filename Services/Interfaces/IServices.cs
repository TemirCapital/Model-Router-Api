using ModelRouterApi.Models;

namespace ModelRouterApi.Services.Interfaces;

public interface IIntentClassificationService
{
    /// <summary>
    /// Classifies the user prompt and returns a routing intent with confidence signals.
    /// </summary>
    Task<Intent> ClassifyAsync(string prompt, RoutingHint? hint = null);
}

public interface IModelService
{
    /// <summary>Tier 1 - gpt-4o-mini. Short factual queries.</summary>
    Task<(string Answer, int TokensUsed)> CallFastModelAsync(string prompt);

    /// <summary>Tier 2 - gpt-4.1-mini. Multi-step, how-to queries.</summary>
    Task<(string Answer, int TokensUsed)> CallModerateModelAsync(string prompt);

    /// <summary>Tier 3 - gpt-4o. Analytical, comparison, summarization.</summary>
    Task<(string Answer, int TokensUsed)> CallReasoningModelAsync(string prompt);

    /// <summary>Tier 4 - gpt-4.1. Complex, multi-domain, architecture.</summary>
    Task<(string Answer, int TokensUsed)> CallComplexModelAsync(string prompt);

    /// <summary>Tier 5 - o4-mini. Critical incidents, root cause, chain-of-thought.</summary>
    Task<(string Answer, int TokensUsed)> CallCriticalModelAsync(string prompt);

    Task<(string Answer, int TokensUsed)> SummarizeToolResultAsync(string toolData, string originalPrompt);
    Task<(float[] Embeddings, int TokensUsed)> GetEmbeddingsAsync(string text);
}

public interface IToolService
{
    /// <summary>
    /// Determines which registered tool matches the intent and executes it.
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(string toolName, string prompt);

    /// <summary>
    /// Returns all registered tool names for discovery (Copilot Studio / Foundry manifest).
    /// </summary>
    IReadOnlyList<string> GetRegisteredTools();
}

public interface ICostCalculatorService
{
    decimal Calculate(IntentType intentType, int tokensUsed);
}
