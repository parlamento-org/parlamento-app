using Parlamento.Application.Documents;
using Parlamento.Domain.Documents;

namespace Parlamento.Application.Abstractions;

public interface IDocumentModelRenderer
{
    string RendererVersion { get; }

    DocumentRenderResult Render(ParliamentDocumentModel document);
}
