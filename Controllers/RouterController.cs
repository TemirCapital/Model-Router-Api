using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ModelRouterApi.Configuration;
using ModelRouterApi.Models;
using ModelRouterApi.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ModelRouterApi.Controllers;

/// <summary>
/// Central routing endpoint consumed by:
///   - Microsoft Copilot Studio (via custom connector)
///   - Azure AI Foundry agents (via tool definition)
///   - Any HTTP client in your enterprise stack
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RouterController : ControllerBase
{
    private readonly IIntentClassificationService _intentService;
    private readonly IModelService _modelService;
    private readonly IToolService _toolService;
    private readonly ICostCalculatorService _costService;
    private readonly ModelRouterOptions _options;
    private readonly ILogger<RouterController> _logger;

    public RouterController(
        IIntentClassificationService intentService,
        IModelService modelService,
        IToolService toolService,
        ICostCalculatorService costService,
        IOptions<ModelRouterOptions> options,
        ILogger<RouterController> logger)
    {
        _intentService = intentService;
        _modelService = modelService;
        _toolService = toolService;
        _costService = costService;
        _options = options.Value;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/router/route
    // Primary routing endpoint — classifies and dispatches to right model/tool
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("route")]
    [ProducesResponseType(typeof(PromptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RoutePrompt([FromBody] PromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = "Prompt text cannot be empty." });

        var sw = Stopwatch.StartNew();
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? "N/A";

        _logger.LogInformation(
            "[{CorrelationId}] Routing prompt (length={Len}) SessionId={Session}",
            correlationId, request.Text.Length, request.SessionId ?? "none");

        try
        {
            // Step 1: Classify intent
            var intent = await _intentService.ClassifyAsync(request.Text, request.RoutingHint);

            // Step 2: Apply minimum confidence guard — fall back to Reasoning
            var effectiveIntentType = intent.Confidence >= _options.RoutingThresholds.MinConfidenceThreshold
                ? intent.Type
                : IntentType.Analytical;

            string answer;
            int tokensUsed;
            string modelUsed;
            ToolExecutionResult? toolResult = null;

            // Step 3: Dispatch
            switch (effectiveIntentType)
            {
                case IntentType.Simple:
                    modelUsed = _options.AzureOpenAi.FastModelDeployment;
                    (answer, tokensUsed) = await _modelService.CallFastModelAsync(request.Text);
                    break;

                case IntentType.Business:
                    // Tool-first: fetch data → then summarise with model
                    var toolName = intent.Tool ?? "GenericActionTool";
                    toolResult = await _toolService.ExecuteAsync(toolName, request.Text);
                    modelUsed = _options.AzureOpenAi.ReasoningModelDeployment;

                    if (toolResult.Success)
                        (answer, tokensUsed) = await _modelService.SummarizeToolResultAsync(
                            toolResult.RawData, request.Text);
                    else
                        (answer, tokensUsed) = ($"I was unable to retrieve the information: {toolResult.RawData}", 0);
                    break;

                case IntentType.Analytical:
                case IntentType.Unknown:
                default:
                    modelUsed = _options.AzureOpenAi.ReasoningModelDeployment;
                    (answer, tokensUsed) = await _modelService.CallReasoningModelAsync(request.Text);
                    break;
            }

            sw.Stop();

            var response = new PromptResponse
            {
                Answer = answer,
                LatencyMs = sw.ElapsedMilliseconds,
                TokensUsed = tokensUsed,
                EstimatedCostUsd = _costService.Calculate(effectiveIntentType, tokensUsed),
                ToolResult = toolResult,
                RoutingDecision = new RoutingDecision
                {
                    IntentType = effectiveIntentType,
                    ModelUsed = modelUsed,
                    ConfidenceScore = intent.Confidence,
                    MatchedSignals = intent.Signals,
                    Rationale = BuildRationale(effectiveIntentType, intent)
                }
            };

            _logger.LogInformation(
                "[{CorrelationId}] Routed to {Intent}/{Model} | Tokens={Tokens} | Cost=${Cost} | Latency={Latency}ms",
                correlationId, effectiveIntentType, modelUsed, tokensUsed,
                response.EstimatedCostUsd, sw.ElapsedMilliseconds);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Routing failed", correlationId);
            return StatusCode(500, new { error = "An error occurred during model routing.", correlationId });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/router/tools
    // Tool discovery — used by Copilot Studio connector and Foundry manifests
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("tools")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult GetRegisteredTools()
    {
        return Ok(new
        {
            tools = _toolService.GetRegisteredTools(),
            count = _toolService.GetRegisteredTools().Count
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/router/health
    // Lightweight liveness check for APIM / container health probes
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() =>
        Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow });

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string BuildRationale(IntentType type, Models.Intent intent) =>
        type switch
        {
            IntentType.Simple =>
                $"Prompt is short ({intent.TokenEstimate} tokens) with no complex signals. " +
                "Fast model selected to minimise cost.",
            IntentType.Analytical =>
                $"Detected analytical signals: [{string.Join(", ", intent.Signals)}]. " +
                "Reasoning model selected for depth.",
            IntentType.Business =>
                $"Detected business action signals: [{string.Join(", ", intent.Signals)}]. " +
                $"Tool '{intent.Tool}' invoked first, then result summarised.",
            IntentType.Unknown =>
                "Intent unclear. Conservatively routed to Reasoning model.",
            _ => "Standard routing applied."
        };
}
