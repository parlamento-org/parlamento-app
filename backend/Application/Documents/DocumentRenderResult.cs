namespace Parlamento.Application.Documents;

public record DocumentRenderResult(
    string Html,
    string PlainText,
    string RendererVersion);
