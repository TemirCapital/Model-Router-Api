using ModelRouterApi.Configuration;
using ModelRouterApi.Models;
using ModelRouterApi.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ModelRouterApi.Services;

public class CostCalculatorService : ICostCalculatorService
{
    private readonly ModelCostOptions _costs;

    public CostCalculatorService(IOptions<ModelRouterOptions> options)
    {
        _costs = options.Value.ModelCosts;
    }

    public decimal Calculate(IntentType intentType, int tokensUsed)
    {
        var costPer1K = intentType switch
        {
            IntentType.Simple  => _costs.FastModelCostPer1KTokens,
            IntentType.Analytical or IntentType.Unknown => _costs.ReasoningModelCostPer1KTokens,
            IntentType.Business => _costs.ReasoningModelCostPer1KTokens, // summariser uses reasoning
            IntentType.Embedding => _costs.EmbeddingModelCostPer1KTokens,
            _ => _costs.ReasoningModelCostPer1KTokens
        };

        return Math.Round((tokensUsed / 1000m) * costPer1K, 8);
    }
}
