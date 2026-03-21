using ModelRouterApi.Extensions;
using ModelRouterApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Model Router API",
        Version = "v1",
        Description =
            "Cost-aware multi-model AI orchestration layer. " +
            "Consumed by Microsoft Copilot Studio, Azure AI Foundry, and .NET clients."
    });
});

// ── Model Router (all router services registered here) ───────────────────────
builder.Services.AddModelRouter(builder.Configuration);

// ── CORS — adjust origins for your Copilot Studio / Foundry integration ──────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEnterpriseClients", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["https://make.powerautomate.com", "https://copilotstudio.microsoft.com"])
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Application Insights (optional — uncomment in production) ─────────────────
// builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Model Router API v1"));
}

app.UseMiddleware<RoutingTelemetryMiddleware>();
app.UseCors("AllowEnterpriseClients");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
