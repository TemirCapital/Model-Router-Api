using Azure;
using Azure.AI.OpenAI;
using ModelRouterApi.Configuration;
using ModelRouterApi.Services.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace ModelRouterApi.Services;

/// <summary>
/// Wraps Azure OpenAI client calls for each model tier.
/// Each method targets a specific deployment — callers never deal with
/// deployment names or API specifics directly.
/// </summary>
public class ModelService : IModelService
{
    private readonly AzureOpenAIClient _client;
    private readonly AzureOpenAiOptions _aiOptions;
    private readonly ILogger<ModelService> _logger;

    public ModelService(
        IOptions<ModelRouterOptions> options,
        ILogger<ModelService> logger)
    {
        _aiOptions = options.Value.AzureOpenAi;
        _logger = logger;

        _client = new AzureOpenAIClient(
            new Uri(_aiOptions.Endpoint),
            new AzureKeyCredential(_aiOptions.ApiKey));
    }

    // ── Fast Model (e.g. gpt-4o-mini) ────────────────────────────────────────

    public async Task<(string Answer, int TokensUsed)> CallFastModelAsync(string prompt)
    {
        _logger.LogInformation("Routing to FastModel [{Deployment}]", _aiOptions.FastModelDeployment);

        var chatClient = _client.GetChatClient(_aiOptions.FastModelDeployment);
        var response = await chatClient.CompleteChatAsync(
        [
            new SystemChatMessage("You are a helpful enterprise assistant. Be concise and accurate."),
            new UserChatMessage(prompt)
        ]);

        var answer = response.Value.Content[0].Text;
        var tokens = response.Value.Usage.TotalTokenCount;

        _logger.LogInformation("FastModel response: {Tokens} tokens used", tokens);
        return (answer, tokens);
    }

    // ── Reasoning Model (e.g. gpt-4o) ────────────────────────────────────────

    public async Task<(string Answer, int TokensUsed)> CallReasoningModelAsync(string prompt)
    {
        _logger.LogInformation("Routing to ReasoningModel [{Deployment}]", _aiOptions.ReasoningModelDeployment);

        var chatClient = _client.GetChatClient(_aiOptions.ReasoningModelDeployment);
        var options = new ChatCompletionOptions
        {
            Temperature = 0.2f  // lower temperature for analytical accuracy
        };

        var response = await chatClient.CompleteChatAsync(
        [
            new SystemChatMessage(
                "You are a senior enterprise analyst. Provide thorough, structured responses " +
                "with clear reasoning. Use bullet points or numbered lists where appropriate."),
            new UserChatMessage(prompt)
        ], options);

        var answer = response.Value.Content[0].Text;
        var tokens = response.Value.Usage.TotalTokenCount;

        _logger.LogInformation("ReasoningModel response: {Tokens} tokens used", tokens);
        return (answer, tokens);
    }

    // ── Embedding Model (e.g. text-embedding-3-small) ────────────────────────

    public async Task<(float[] Embeddings, int TokensUsed)> GetEmbeddingsAsync(string text)
    {
        _logger.LogInformation("Calling EmbeddingModel [{Deployment}]", _aiOptions.EmbeddingModelDeployment);

        var embeddingClient = _client.GetEmbeddingClient(_aiOptions.EmbeddingModelDeployment);
        var response = await embeddingClient.GenerateEmbeddingsAsync([text]);

        var vector = response.Value[0].ToFloats().ToArray();
        var tokens = response.Value.Usage.InputTokenCount;

        _logger.LogInformation("EmbeddingModel: {Tokens} tokens, vector dim={Dim}", tokens, vector.Length);
        return (vector, tokens);
    }

    // ── Tool result summariser (always uses Reasoning for quality) ────────────

    public async Task<(string Answer, int TokensUsed)> SummarizeToolResultAsync(
        string toolData, string originalPrompt)
    {
        _logger.LogInformation("Summarising tool result with ReasoningModel");

        var chatClient = _client.GetChatClient(_aiOptions.ReasoningModelDeployment);
        var systemPrompt =
            "You are an enterprise assistant. You have retrieved structured data from a business system. " +
            "Summarize this data in a clear, user-friendly way that directly answers the original request. " +
            "Be concise but complete.";

        var userContent =
            $"Original request: {originalPrompt}\n\n" +
            $"Data retrieved from business system:\n{toolData}";

        var response = await chatClient.CompleteChatAsync(
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userContent)
        ]);

        var answer = response.Value.Content[0].Text;
        var tokens = response.Value.Usage.TotalTokenCount;
        return (answer, tokens);
    }
}
