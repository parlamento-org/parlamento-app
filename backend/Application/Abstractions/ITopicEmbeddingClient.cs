namespace Parlamento.Application.Abstractions;

public interface ITopicEmbeddingClient
{
    string Provider { get; }

    string ModelName { get; }

    Task<IReadOnlyList<IReadOnlyList<double>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}
