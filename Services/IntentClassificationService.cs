using ModelRouterApi.Configuration;
using ModelRouterApi.Models;
using ModelRouterApi.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ModelRouterApi.Services;

/// <summary>
/// Hybrid intent classifier:
///   Phase 1 — Fast rule-based classification using token count + keyword signals.
///   Phase 2 (extension point) — ML/LLM-based classification when confidence is low.
///
/// Design rationale: We avoid calling an LLM just to classify, since that itself
/// costs tokens. Rule-based is fast, free, and surprisingly accurate for enterprise prompts.
/// </summary>
public class IntentClassificationService(
    IOptions<ModelRouterOptions> options,
    ILogger<IntentClassificationService> logger) : IIntentClassificationService
{
    private readonly ModelRouterOptions _options = options.Value;
    private readonly ILogger<IntentClassificationService> _logger = logger;

    // ── Keyword signal tables ──────────────────────────────────────────────────
    private static readonly HashSet<string> AnalyticalKeywords =
    [
        "compare", "explain", "summarize", "why", "how does", "analyze",
        "difference", "pros and cons", "evaluate", "describe", "elaborate",
        "breakdown", "review", "assess", "what is", "tell me about"
    ];

    private static readonly HashSet<string> BusinessKeywords =
    [
        "create", "update", "delete", "show customer", "check order",
        "get order", "find customer", "open case", "close case", "raise ticket",
        "book", "schedule", "cancel", "fetch", "retrieve", "list orders",
        "customer details", "account info"
    ];

    private static readonly Dictionary<string, string> BusinessToolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "check order",     "OrderManagementTool" },
        { "get order",       "OrderManagementTool" },
        { "list orders",     "OrderManagementTool" },
        { "show customer",   "CrmTool" },
        { "find customer",   "CrmTool" },
        { "customer details","CrmTool" },
        { "account info",    "CrmTool" },
        { "open case",       "CaseTool" },
        { "close case",      "CaseTool" },
        { "raise ticket",    "CaseTool" },
        { "book",            "SchedulerTool" },
        { "schedule",        "SchedulerTool" },
        { "cancel",          "SchedulerTool" },
        { "create",          "GenericActionTool" },
        { "update",          "GenericActionTool" },
        { "delete",          "GenericActionTool" },
    };

    public Task<Intent> ClassifyAsync(string prompt, RoutingHint? hint = null)
    {
        // ── Caller override via routing hint (Copilot Studio can pass this) ──
        if (hint?.ForceReasoning == true)
            return Task.FromResult(BuildIntent(IntentType.Analytical, 0.99f, [], null, prompt));

        if (hint?.ForceFast == true)
            return Task.FromResult(BuildIntent(IntentType.Simple, 0.99f, [], null, prompt));

        var normalised = prompt.Trim().ToLowerInvariant();
        var tokenEstimate = EstimateTokens(prompt);
        var matchedSignals = new List<string>();

        // ── Rule 1: Business action detection (highest priority) ──────────────
        var (isBusinessIntent, toolName, businessSignals) = DetectBusinessIntent(normalised);
        if (isBusinessIntent)
        {
            _logger.LogDebug("Intent: Business | Tool: {Tool} | Signals: {Signals}",
                toolName, string.Join(", ", businessSignals));
            return Task.FromResult(
                BuildIntent(IntentType.Business, 0.92f, businessSignals, toolName, prompt, tokenEstimate));
        }

        // ── Rule 2: Analytical / reasoning detection ───────────────────────────
        var (isAnalytical, analyticalSignals) = DetectAnalyticalIntent(normalised);
        if (isAnalytical)
        {
            // "what is X" on a short prompt is a simple lookup, not deep analysis.
            // Only escalate to Reasoning if the prompt is substantive enough to warrant it.
            if (tokenEstimate <= _options.RoutingThresholds.SimpleTokenThreshold
                && analyticalSignals.All(s => s is "what is" or "tell me about"))
            {
                return Task.FromResult(
                    BuildIntent(IntentType.Simple, 0.75f, analyticalSignals, null, prompt, tokenEstimate));
            }

            _logger.LogDebug("Intent: Analytical | Signals: {Signals}",
                string.Join(", ", analyticalSignals));
            return Task.FromResult(
                BuildIntent(IntentType.Analytical, 0.85f, analyticalSignals, null, prompt, tokenEstimate));
        }

        // ── Rule 3: Simple / fast-model detection ─────────────────────────────
        if (tokenEstimate <= _options.RoutingThresholds.SimpleTokenThreshold)
        {
            matchedSignals.Add($"token_count={tokenEstimate} (below threshold={_options.RoutingThresholds.SimpleTokenThreshold})");
            return Task.FromResult(
                BuildIntent(IntentType.Simple, 0.80f, matchedSignals, null, prompt, tokenEstimate));
        }

        // ── Fallback: Unknown → route conservatively to Reasoning ─────────────
        _logger.LogDebug("Intent: Unknown — falling back to Reasoning model");
        return Task.FromResult(
            BuildIntent(IntentType.Unknown, 0.40f, [], null, prompt, tokenEstimate));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (bool IsMatch, string? ToolName, List<string> Signals) DetectBusinessIntent(string prompt)
    {
        var signals = new List<string>();
        string? resolvedTool = null;

        foreach (var (keyword, tool) in BusinessToolMap)
        {
            if (prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                signals.Add(keyword);
                resolvedTool ??= tool; // first match wins for tool selection
            }
        }

        return (signals.Count > 0, resolvedTool, signals);
    }

    private static (bool IsMatch, List<string> Signals) DetectAnalyticalIntent(string prompt)
    {
        var signals = AnalyticalKeywords
            .Where(k => prompt.Contains(k, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return (signals.Count > 0, signals);
    }

    private static Intent BuildIntent(
        IntentType type,
        float confidence,
        List<string> signals,
        string? tool,
        string prompt,
        int tokenEstimate = 0) => new()
    {
        Type = type,
        Confidence = confidence,
        Tool = tool,
        Signals = signals,
        TokenEstimate = tokenEstimate > 0 ? tokenEstimate : EstimateTokens(prompt)
    };

    /// <summary>
    /// Lightweight token estimator: ~4 chars per token (GPT-4 heuristic).
    /// Replace with TikToken for precision in production.
    /// </summary>
    private static int EstimateTokens(string text) =>
        Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
}
