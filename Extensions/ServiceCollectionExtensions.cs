using ModelRouterApi.Configuration;
using ModelRouterApi.Services;
using ModelRouterApi.Services.Interfaces;
using ModelRouterApi.Tools;

namespace ModelRouterApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModelRouter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Strongly-typed config ─────────────────────────────────────────────
        services.Configure<ModelRouterOptions>(
            configuration.GetSection(ModelRouterOptions.SectionName));

        // ── Core routing services ─────────────────────────────────────────────
        services.AddScoped<IIntentClassificationService, IntentClassificationService>();
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<IToolService, ToolService>();
        services.AddScoped<ICostCalculatorService, CostCalculatorService>();

        // ── Business tool registry (add new tools here — no other changes needed) ──
        services.AddScoped<IBusinessTool, CrmTool>();
        services.AddScoped<IBusinessTool, OrderManagementTool>();
        services.AddScoped<IBusinessTool, CaseTool>();
        services.AddScoped<IBusinessTool, SchedulerTool>();
        services.AddScoped<IBusinessTool, GenericActionTool>();

        return services;
    }
}
