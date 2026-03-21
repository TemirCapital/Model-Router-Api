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
    Task<(string Answer, int TokensUsed)> CallFastModelAsync(string prompt);
    Task<(string Answer, int TokensUsed)> CallReasoningModelAsync(string prompt);
    Task<(float[] Embeddings, int TokensUsed)> GetEmbeddingsAsync(string text);
    Task<(string Answer, int TokensUsed)> SummarizeToolResultAsync(string toolData, string originalPrompt);
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
