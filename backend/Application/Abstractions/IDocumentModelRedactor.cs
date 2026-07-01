using Parlamento.Domain.Documents;

namespace Parlamento.Application.Abstractions;

public interface IDocumentModelRedactor
{
    string PolicyVersion { get; }

    ParliamentDocumentModel Redact(
        ParliamentDocumentModel document,
        IEnumerable<string> terms);
}
