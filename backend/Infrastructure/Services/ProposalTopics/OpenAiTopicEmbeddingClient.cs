using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using Parlamento.Application.Abstractions;

namespace Parlamento.Infrastructure.Services.ProposalTopics;

public class OpenAiTopicEmbeddingClient : ITopicEmbeddingClient
{
    private static readonly Uri EmbeddingsUri = new("https://api.openai.com/v1/embeddings");
    private readonly HttpClient _httpClient;
    private readonly ProposalTopicOptions _options;

    public OpenAiTopicEmbeddingClient(
        HttpClient httpClient,
        IOptions<ProposalTopicOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string Provider => "openai";

    public string ModelName => _options.EmbeddingModel;

    public async Task<IReadOnlyList<IReadOnlyList<double>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Set OPENAI_API_KEY or ProposalTopics:ApiKey.");
        }

        if (inputs.Count == 0)
        {
            return [];
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, EmbeddingsUri)
        {
            Content = JsonContent.Create(new EmbeddingRequest(_options.EmbeddingModel, inputs))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI Embeddings request failed with status {(int)response.StatusCode}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<EmbeddingResponse>(body, JsonOptions)
                     ?? throw new InvalidOperationException("OpenAI embedding response was empty or invalid JSON.");

        return parsed.Data
            .OrderBy(x => x.Index)
            .Select(x => (IReadOnlyList<double>)x.Embedding)
            .ToList();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private record EmbeddingRequest(string Model, IReadOnlyList<string> Input);

    private record EmbeddingResponse(IReadOnlyList<EmbeddingData> Data);

    private record EmbeddingData(
        int Index,
        [property: JsonPropertyName("embedding")] double[] Embedding);
}
