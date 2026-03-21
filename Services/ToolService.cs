using ModelRouterApi.Models;
using ModelRouterApi.Services.Interfaces;
using ModelRouterApi.Tools;

namespace ModelRouterApi.Services;

/// <summary>
/// Registry-based tool execution service.
/// Tools are registered via DI as IBusinessTool implementations.
/// This avoids a giant switch statement and makes it easy to add new tools
/// without touching the router — open/closed principle.
/// </summary>
public class ToolService : IToolService
{
    private readonly IReadOnlyDictionary<string, IBusinessTool> _tools;
    private readonly ILogger<ToolService> _logger;

    public ToolService(
        IEnumerable<IBusinessTool> tools,
        ILogger<ToolService> logger)
    {
        _tools = tools.ToDictionary(t => t.ToolName, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, string prompt)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            _logger.LogWarning("Tool [{ToolName}] not registered. Falling back to GenericActionTool.", toolName);
            toolName = "GenericActionTool";

            if (!_tools.TryGetValue(toolName, out tool))
            {
                return new ToolExecutionResult
                {
                    ToolName = toolName,
                    Success = false,
                    RawData = $"Tool '{toolName}' is not registered in this environment."
                };
            }
        }

        try
        {
            _logger.LogInformation("Executing tool [{ToolName}] for prompt snippet: {Snippet}",
                toolName, prompt[..Math.Min(60, prompt.Length)]);

            var result = await tool.ExecuteAsync(prompt);
            return new ToolExecutionResult
            {
                ToolName = toolName,
                Success = true,
                RawData = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool [{ToolName}] execution failed", toolName);
            return new ToolExecutionResult
            {
                ToolName = toolName,
                Success = false,
                RawData = $"Tool execution failed: {ex.Message}"
            };
        }
    }

    public IReadOnlyList<string> GetRegisteredTools() =>
        [.. _tools.Keys];
}
