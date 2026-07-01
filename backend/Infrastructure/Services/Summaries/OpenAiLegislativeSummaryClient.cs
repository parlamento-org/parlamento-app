using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Parlamento.Application.Abstractions;
using Parlamento.Application.Summaries;

namespace Parlamento.Infrastructure.Services.Summaries;

public class OpenAiLegislativeSummaryClient : ILegislativeSummaryClient
{
    private static readonly Uri ChatCompletionsUri = new("https://api.openai.com/v1/chat/completions");
    private readonly HttpClient _httpClient;
    private readonly OpenAiSummaryOptions _options;
    private readonly ILogger<OpenAiLegislativeSummaryClient> _logger;

    public OpenAiLegislativeSummaryClient(
        HttpClient httpClient,
        IOptions<OpenAiSummaryOptions> options,
        ILogger<OpenAiLegislativeSummaryClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ModelName => _options.Model;

    public string PromptVersion => LegislativeSummaryPrompt.Version;

    public async Task<GeneratedParliamentSummary> GenerateSummaryAsync(
        string redactedPlainText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Set OPENAI_API_KEY.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUri)
        {
            Content = JsonContent.Create(new ChatCompletionRequest(
                _options.Model,
                _options.Temperature,
                _options.MaxOutputTokens,
                [
                    new ChatMessage("system", LegislativeSummaryPrompt.SystemPrompt),
                    new ChatMessage(
                        "user",
                        $"{LegislativeSummaryPrompt.UserPromptPrefix}\n\n{redactedPlainText}")
                ],
                new ResponseFormat("json_object")))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI Chat Completions request failed with status {(int)response.StatusCode}: {body}");
        }

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonOptions)
                         ?? throw new InvalidOperationException("OpenAI response was empty or invalid JSON.");
        var content = completion.Choices.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("OpenAI response did not contain summary content.");
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SummaryPayload>(content, JsonOptions)
                          ?? throw new InvalidOperationException("Summary payload was empty.");

            var summary = payload.Summary?.Trim();
            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new InvalidOperationException("Summary payload did not contain a summary field.");
            }

            return new GeneratedParliamentSummary(
                string.IsNullOrWhiteSpace(payload.Title) ? null : payload.Title.Trim(),
                summary,
                payload.BulletPoints?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList() ?? []);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "OpenAI returned non-parseable summary JSON: {Content}", content);
            throw new InvalidOperationException("OpenAI returned non-parseable summary JSON.", ex);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private record ChatCompletionRequest(
        string Model,
        double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("response_format")] ResponseFormat ResponseFormat);

    private record ChatMessage(string Role, string Content);

    private record ResponseFormat(string Type);

    private record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private record ChatChoice(ChatChoiceMessage? Message);

    private record ChatChoiceMessage(string? Content);

    private record SummaryPayload(
        string? Title,
        string? Summary,
        [property: JsonPropertyName("bullet_points")] IReadOnlyList<string>? BulletPoints);
}
