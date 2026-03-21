# Model Router API — POC

A cost-aware, multi-model AI orchestration layer built on **.NET 9** and **Azure OpenAI**.  
Designed to sit between Copilot Studio / Azure AI Foundry and your Azure OpenAI deployments,  
routing every prompt to the cheapest model that can correctly answer it.

---

## Architecture

```
                      User
                       │
          ┌────────────┴───────────────┐
          │      Copilot Studio        │  ◄── Custom Connector (OpenAPI)
          └────────────┬───────────────┘
                       │ Tool Call / HTTP POST
                       ▼
          ┌────────────────────────────┐
          │   .NET Model Router API    │  ← YOU ARE HERE
          │   POST /api/router/route   │
          └────┬────────┬────────┬────┘
               │        │        │
               ▼        ▼        ▼
          Fast Model  Reasoning  Embedding
         (gpt-4o-mini) (gpt-4o)  (ada-3-small)
                       │
                       ▼
             Business Tool APIs
            (CRM / Orders / Cases)

          Also reachable from:
          ┌──────────────────────────┐
          │   Azure AI Foundry Agent │  ◄── Tool definition → same endpoint
          └──────────────────────────┘
```

---

## Routing Logic

| Signal | Intent | Model Selected |
|---|---|---|
| Prompt < 20 tokens, no special keywords | `Simple` | gpt-4o-mini |
| Keywords: compare, explain, summarize, why, analyze… | `Analytical` | gpt-4o |
| Keywords: show customer, check order, open case, create, update… | `Business` | Tool → gpt-4o |
| Unknown / low confidence | `Unknown` | gpt-4o (safe fallback) |

Cost comparison (hypothetical daily load of 10,000 prompts):

| Scenario | Daily Cost |
|---|---|
| All prompts on gpt-4o | ~$100/day |
| Routed architecture | ~$38/day |

---

## Project Structure

```
ModelRouterApi/
├── Controllers/
│   └── RouterController.cs         ← Main routing endpoint
├── Models/
│   ├── PromptRequest.cs            ← Request DTO (consumed by Copilot Studio)
│   ├── PromptResponse.cs           ← Response DTO with routing telemetry
│   └── Intent.cs                   ← Classification result
├── Services/
│   ├── Interfaces/IServices.cs     ← All service contracts
│   ├── IntentClassificationService.cs  ← Rule-based hybrid classifier
│   ├── ModelService.cs             ← Azure OpenAI calls per model tier
│   ├── ToolService.cs              ← Registry-based tool executor
│   └── CostCalculatorService.cs    ← Per-request USD cost estimation
├── Tools/
│   └── BusinessTools.cs            ← CRM, Order, Case, Scheduler, Generic tools
├── Configuration/
│   └── ModelRouterOptions.cs       ← Strongly-typed config (no magic strings)
├── Extensions/
│   └── ServiceCollectionExtensions.cs  ← Clean DI registration
├── Middleware/
│   └── RoutingTelemetryMiddleware.cs   ← Correlation ID + latency logging
├── appsettings.json
├── Dockerfile
├── ModelRouterApi.http             ← VS Code / Rider HTTP test file
└── copilot-studio-connector.json   ← Import directly into Copilot Studio
```

---

## Quick Start

### 1. Prerequisites

- .NET 9 SDK
- Azure OpenAI resource with three deployments:
  - `gpt-4o-mini` → FastModel
  - `gpt-4o` → ReasoningModel
  - `text-embedding-3-small` → EmbeddingModel

### 2. Configure

Edit `appsettings.json`:

```json
"ModelRouter": {
  "AzureOpenAi": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "ApiKey": "<your-api-key>",
    "FastModelDeployment": "gpt-4o-mini",
    "ReasoningModelDeployment": "gpt-4o",
    "EmbeddingModelDeployment": "text-embedding-3-small"
  }
}
```

> **Security note:** In production use Azure Key Vault + Managed Identity instead of `ApiKey` in config.

### 3. Run

```bash
cd ModelRouterApi
dotnet restore
dotnet run
```

Swagger UI → `https://localhost:7001/swagger`

### 4. Test

Use `ModelRouterApi.http` in VS Code REST Client or JetBrains Rider.

---

## Integrating with Copilot Studio

1. Open **Copilot Studio** → **Custom Connectors** → **Import from OpenAPI**
2. Upload `copilot-studio-connector.json`
3. Set the host to your deployed API URL
4. In your Copilot topic, add a **Call an action** step → Select `RoutePrompt`
5. Map `System.Activity.Text` → `text` field in the request
6. Surface `answer` from the response back to the user

---

## Integrating with Azure AI Foundry

1. Create an **Agent** in Azure AI Foundry
2. Add a **Tool** with type `OpenApi`
3. Point the tool spec URL at `https://<your-api>/swagger/v1/swagger.json`
4. Select the `RoutePrompt` operation
5. The Foundry agent will call your orchestrator for every user turn

---

## Adding a New Business Tool

1. Create a class in `Tools/BusinessTools.cs` implementing `IBusinessTool`
2. Register it in `Extensions/ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddScoped<IBusinessTool, YourNewTool>();
   ```
3. Add keyword → tool name mapping in `IntentClassificationService.BusinessToolMap`

No changes needed in the controller or router — open/closed principle.

---

## Production Hardening Checklist

- [ ] Replace `ApiKey` in config with **Azure Key Vault** reference
- [ ] Enable **Managed Identity** on Azure OpenAI (remove key entirely)
- [ ] Add **Azure API Management** in front for rate limiting, auth, and analytics
- [ ] Enable **Application Insights** — set `ApplicationInsightsConnectionString`
- [ ] Replace mock tools with real **HttpClient** calls to CRM / ERP APIs
- [ ] Add **TikToken** for precise token counting (replaces heuristic estimator)
- [ ] Add **distributed cache** (Redis) for embedding results
- [ ] Add **circuit breaker** (Polly) on model and tool HTTP calls
- [ ] Configure **Azure Container Apps** or **AKS** for auto-scaling

---

## Extending the Router

### Add ML-based Intent Classification
Replace or augment `IntentClassificationService` with a call to:
- Azure AI Language (custom text classification)
- A fine-tuned embedding similarity classifier
- A lightweight local ONNX model

The `IIntentClassificationService` interface is your extension point — swap the implementation in DI, zero controller changes.

### Adaptive Routing (Future)
Capture `success = false` signals from tool results and model answers,
feed them into a feedback store, and adjust routing thresholds dynamically.
This is the "real-time telemetry + adaptive routing" enhancement mentioned in the blog.
