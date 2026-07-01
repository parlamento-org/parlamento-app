namespace Parlamento.Application.Imports;

public record ParliamentBaseInfoImportResult(
    string Legislature,
    int DeputiesRead,
    int DeputiesUpserted,
    int ParliamentaryGroupsRead,
    int ParliamentaryGroupsUpserted,
    int RedactionTermsRebuilt);
