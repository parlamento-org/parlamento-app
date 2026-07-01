# Scraper Migration Specification

This document describes the current `parlamento-scraper` repository and how to migrate its useful behavior into `parlamento-app`, whose backend is an ASP.NET Core API backed by PostgreSQL.

No live Portuguese Parliament Open Data endpoints were called while producing this spec. The JSON contract analysis is based on the local sample files [example_iniciativa.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/example_iniciativa.json) and [example_projeto_lei.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/example_projeto_lei.json). The repository also contains endpoint URL configuration files, and those URLs are documented here as configured data sources, not as inspected live responses.

## 1. Current Scraper Behavior

The repository is a small Node.js scraper/import script. Its main workflow lives in [index.js](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/index.js).

At a high level, `index.js`:

1. Loads an initiative JSON feed URL from [proposals_links.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/proposals_links.json).
2. Fetches the full legislature initiative JSON.
3. Filters initiatives to `"Projeto de Lei"`.
4. Further filters to initiatives that have an event with `CodigoFase === "250"` (`Votacao na generalidade`).
5. Skips initiatives not authored by parliamentary groups (`IniAutorGruposParlamentares` missing/null).
6. Checks a local API for duplicates via `GET http://localhost:8080/proposal/source/{IniId}`.
7. Extracts generality and final/global vote objects from `IniEventos`.
8. Downloads the proposal document from `IniLinkTexto`.
9. Converts that document/PDF to HTML with `pdf2html`.
10. Removes/censors known party and deputy names from the HTML.
11. POSTs a transformed proposal payload to `POST http://localhost:8080/proposal/`.

The script is currently hard-coded to legislature `XIII`:

```js
// index.js:341-343
const leg = 'XIII';
console.log("Processing legislature " + leg);
processLegislature(proposal_links["legislaturas"][leg], leg);
```

The current importer entry point is:

```js
// index.js:315-338
async function processLegislature(leg_link, leg_arg) {
  const response = await fetch(leg_link);
  const jsonData = await response.json();
  const iniciativasData = jsonData;

  let projetosLei = iniciativasData.filter(
    (iniciativa) => iniciativa["IniDescTipo"] === "Projeto de Lei"
  );

  projetosLei = projetosLei.filter(
    (iniciativa) =>
      Array.isArray(iniciativa["IniEventos"]) &&
      iniciativa["IniEventos"].some((evento) => evento["CodigoFase"] === "250")
  );

  const forbidden_words = await loadForbiddenWords(leg_arg);
  await processProjetosLei(projetosLei, forbidden_words);
}
```

Important mismatch with the provided sample: `example_iniciativa.json` is a single initiative with `IniDescTipo: "Proposta de Lei"` and `IniAutorOutros: { nome: "Governo", sigla: "V" }`. The current `index.js` would not import this sample because it only accepts `"Projeto de Lei"` and skips records without `IniAutorGruposParlamentares`.

Additional sample information added later:

- [example_projeto_lei.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/example_projeto_lei.json) is an array with three initiatives.
- Two records are `Projeto de Resolução` (`IniTipo: "R"`), not laws. They still have generality votes and debate links.
- One record is `Projeto de Lei` (`IniTipo: "J"`). It confirms the same top-level JSON structure, but shows a fuller law lifecycle with final global vote, decree, promulgation, referenda, INCM dispatch, and `Lei (Publicação DR)`.
- The new samples confirm that `PublicacaoFase[].URLDiario`, `Intervencoesdebates`, and `Intervencoesdebates[].oradores[].linkVideo[]` must be imported as first-class traceability data.

## 2. Endpoints and Data Sources

### Local sample

- [example_iniciativa.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/example_iniciativa.json)
- Contains one representative `Proposta de Lei` object.
- Root shape in this sample is a single object, not an array.
- [example_projeto_lei.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/example_projeto_lei.json)
- Contains an array of three representative initiatives: two `Projeto de Resolução` records and one `Projeto de Lei`.
- Real legislature files used by `index.js` are expected to be arrays, so the importer should support both single-object fixtures and array feeds.

### Initiative feed URL configuration

- [proposals_links.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/proposals_links.json)
- Maps legislature roman numerals to Open Data initiative JSON document URLs.
- Contains `IV` through `XVII`.
- `index.js` imports this file as `proposal_links`.

There is also [proposal_links_complete.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/proposal_links_complete.json), which is similar but currently covers `II` through `XV`.

The URLs have the form:

```text
https://app.parlamento.pt/webutils/docs/doc.txt?path=...&fich=Iniciativas{LEG}_json.txt&Inline=true
```

The new importer should treat these as configurable source URLs, not hard-coded code constants.

### Legislature/base information URL configuration

- [forbidden_words.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/forbidden_words.json)
- Maps legislatures to Open Data base information JSON document URLs.
- Used by `pdf_propostas.js::loadForbiddenWords`.

The current code expects those base-information JSON files to contain:

```json
{
  "GruposParlamentares": [
    { "sigla": "PSD", "nome": "..." }
  ],
  "Deputados": [
    {
      "DepNomeCompleto": "...",
      "DepNomeParlamentar": "..."
    }
  ]
}
```

### Document/PDF URLs

The sample contains several document URLs:

- `IniLinkTexto`: primary initiative text document.
- `IniAnexos[].anexoFich`: initiative-level attachments.
- `IniEventos[].AnexosFase[].anexoFich`: event/phase attachments.
- `IniEventos[].Comissao[].Documentos[].URL`: commission documents.
- `PublicacaoFase[].URLDiario`: debate/publication pages.

The current script downloads only `IniLinkTexto` for imported proposals.

### Current local/backend API endpoints

These are not source Open Data endpoints; they are the old local API used by the scraper.

- `GET http://localhost:8080/proposal/source/{sourceId}` in `index.js::processProjetosLei` for duplicate checks.
- `POST http://localhost:8080/proposal/` in `index.js::processProjetosLei` to create proposals.
- `GET http://0.0.0.0:8080/proposal/` and `GET http://0.0.0.0:8080/proposal/{id}` in update scripts.
- `PUT http://0.0.0.0:8080/proposal/{id}` in [update_PDFs_already_in_db.js](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/update_PDFs_already_in_db.js).
- `POST http://parlamento-dev-api.fly.dev/proposal/` in [update_live_database.js](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/update_live_database.js).

For `parlamento-app`, this HTTP self-posting pattern should be discarded. The importer should write through application services or EF Core repositories inside the backend process.

## 3. JSON Structures Expected

### Initiative root

The current `processLegislature` assumes the fetched initiative JSON root is an array:

```js
const jsonData = await response.json();
const iniciativasData = jsonData;
let projetosLei = iniciativasData.filter(...);
```

The sample root is a single object. A robust importer should accept both:

- array root: production legislature file.
- object root: sample/test fixture or single-record import.

### Top-level initiative fields in the sample

The sample contains these top-level fields:

```text
DataFimleg
DataInicioleg
IniAnexos
IniAutorDeputados
IniAutorGruposParlamentares
IniAutorOutros
IniciativasEuropeias
IniciativasOrigem
IniciativasOriginadas
IniDescTipo
IniEpigrafe
IniEventos
IniId
IniLeg
IniLinkTexto
IniNr
IniObs
IniSel
IniTextoSubst
IniTextoSubstCampo
IniTipo
IniTitulo
Links
Peticoes
PropostasAlteracao
```

Representative sample values:

```json
{
  "IniId": "356278",
  "IniLeg": "XVII",
  "IniNr": "62",
  "IniTipo": "P",
  "IniDescTipo": "Proposta de Lei",
  "IniSel": "1",
  "DataInicioleg": "2025-06-03",
  "DataFimleg": null,
  "IniTitulo": "Altera a Lei Eleitoral do Presidente da Republica ...",
  "IniLinkTexto": "http://app.parlamento.pt/webutils/docs/doc.pdf?...",
  "IniAutorGruposParlamentares": null,
  "IniAutorDeputados": null,
  "IniAutorOutros": {
    "iniAutorComissao": null,
    "nome": "Governo",
    "sigla": "V"
  }
}
```

Note: The local sample displays mojibake such as `RepÃºblica`. The new importer should read source files as UTF-8 and normalize text encoding where possible. Do not bake mojibake strings into business logic.

### Observed initiative types

The local samples confirm at least these initiative types:

| `IniTipo` | `IniDescTipo` | Meaning for app/importer |
| --- | --- | --- |
| `P` | `Proposta de Lei` | Government bill/proposal. In the sample, author data is under `IniAutorOutros` with `nome: "Governo"`. |
| `J` | `Projeto de Lei` | Bill introduced by parliamentary groups/deputies. Can proceed to final global vote, promulgation, and published law. |
| `R` | `Projeto de Resolução` | Resolution/recommendation. It can have a generality vote and debate, but should not be presented as a binding law. |

For the frontend, avoid a generic label like "law" for every imported initiative. Store and display the source type. A user voting on a `Projeto de Resolução` is reacting to a recommendation/resolution, not a bill that directly becomes a law.

### Attachments

Top-level attachments:

```json
{
  "IniAnexos": [
    {
      "anexoFich": "http://app.parlamento.pt/webutils/docs/doc.pdf?...",
      "anexoNome": "A.I.G."
    }
  ]
}
```

Event-level attachments:

```json
{
  "AnexosFase": [
    {
      "anexoFich": "http://app.parlamento.pt/webutils/docs/doc.pdf?...",
      "anexoNome": "Nota de admissibilidade"
    }
  ]
}
```

### Events/phases

Every initiative can contain `IniEventos`. The sample has 20 events. Important event fields:

```text
ActId
ActividadesConjuntas
AnexosFase
CodigoFase
Comissao
DataFase
EvtId
Fase
IniciativasConjuntas
Intervencoesdebates
Links
ObsFase
OevId
OevTextId
PcpublicasConjuntas
PeticoesConjuntas
PublicacaoFase
RecursoDeputados
RecursoGP
TextosAprovados
Votacao
```

Key phase codes used by current code:

- `"250"`: generality vote (`Votacao na generalidade`).
- `"320"`: final global vote (`Votacao final global`), treated by the old code as `votacaoEspecialidade`.

Additional relevant phase codes from the sample:

- `"10"`: `Entrada`
- `"20"`: `Admissao`
- `"21"`: `Anuncio`
- `"180"`: initial committee distribution / generality
- `"190"`: generality discussion
- `"243"`: appreciation/discussion in the resolution examples
- `"270"`: committee/speciality stage
- `"310"`: speciality vote
- `"348"`: final wording stage
- `"350"`: decree/publication
- `"370"`: dispatch for promulgation
- `"380"`: promulgation
- `"390"`: referenda
- `"400"`: dispatch to INCM
- `"580"`: law publication in Diario da Republica

The old code ignores `"310"` even though one sample has it. The `Projeto de Lei` sample has `250` and `320` but no `310`, so the importer must not assume a fixed `250 -> 310 -> 320` path. It should preserve all events and separately classify stage from `CodigoFase`.

The `Projeto de Lei` sample also shows that the final/global vote can refer to a combined final text for multiple projects. In that example the `320` vote description references both `Projeto de Lei n.º 129/XVII/1.ª (PSD)` and `Projeto de Lei n.º 375/XVII/1.ª (BE)`. This means a user's reaction to the introduced text should not be treated as if it were necessarily a vote on the final law text.

### Votes

The current code expects event votes under `IniEventos[].Votacao`, an array where the first element is used.

Sample generality vote:

```json
{
  "id": "171449",
  "data": "2026-04-17",
  "descricao": null,
  "resultado": "Aprovado",
  "unanime": null,
  "ausencias": null,
  "detalhe": "A Favor: <I>PSD</I>, <I> CH</I>, <I> L</I>, <I> PCP</I>, <I> CDS-PP</I>, <I> BE</I>, <I> PAN</I>, <I> JPP</I><BR>Abstencao:<I>PS</I>, <I> IL</I>",
  "reuniao": "78",
  "tipoReuniao": "RP",
  "publicacao": null
}
```

Sample unanimous speciality vote (`CodigoFase: "310"`):

```json
{
  "id": "172170",
  "data": "2026-06-12",
  "descricao": "Votacao da assuncao pelo Plenario ...",
  "resultado": "Aprovado",
  "unanime": "unanime",
  "ausencias": null,
  "detalhe": null,
  "reuniao": "100",
  "tipoReuniao": "RP",
  "publicacao": null
}
```

Sample final global vote (`CodigoFase: "320"`):

```json
{
  "id": "172171",
  "data": "2026-06-12",
  "descricao": "Texto final, apresentado pela Comissao ...",
  "resultado": "Aprovado",
  "unanime": null,
  "ausencias": null,
  "detalhe": "A Favor: <I>77-PSD</I>, <I> 58-CH</I>, <I> 56-PS</I>, <I> 9-IL</I>, <I> 6-L</I>, <I> 3-PCP</I>, <I> 1-CDS-PP</I>, <I> 1-BE</I>, <I> 1-PAN</I>, <I> 1-JPP</I>",
  "reuniao": "100",
  "tipoReuniao": "RP",
  "publicacao": null
}
```

The vote detail grammar currently supported is a loose HTML-ish string containing sections such as:

```text
A Favor: <I>PSD</I>, <I> CH</I>
Contra: <I>...</I>
Abstencao: <I>PS</I>, <I> IL</I>
Ausencia: <I>...</I>
```

The old parser only handles `A Favor`, `Contra`, and `Abstencao`/mojibake equivalent. It does not model absences as vote blocks.

### Committee structures

The sample contains `IniEventos[].Comissao[]` records. These are not imported by the current scraper, but they are valuable for a real ETL.

Fields observed in the sample:

```text
AccId
Competente
DataAgendamentoPlenario
DataDistribuicao
Documentos[]
IdComissao
Nome
Numero
PareceresRecebidos[]
PedidosParecer[]
Relatores[]
Remessas[]
Votacao[]
```

Nested `Documentos[]` shape:

```json
{
  "DataDocumento": "2026-04-08",
  "TipoDocumento": "Relatorio",
  "TituloDocumento": "Relatorio CACDLG",
  "URL": "http://app.parlamento.pt/webutils/docs/doc.pdf?..."
}
```

Nested `Relatores[]` shape:

```json
{
  "id": "7331",
  "nome": "Rui Rocha",
  "gp": "IL",
  "dataNomeacao": null,
  "dataCessacao": null,
  "motivoCessacao": null
}
```

Committee-level `Votacao[]` appears in the sample under committee distribution and has the same broad shape as event-level `Votacao[]`. The current scraper ignores it.

### Publications/debate links

The samples contain `PublicacaoFase[]` entries:

```json
{
  "pubdt": "2026-04-18",
  "pubLeg": "XVII",
  "pubNr": "80",
  "pubSL": "1",
  "pubTipo": "DAR I serie",
  "pubTp": "D",
  "pag": ["57-57"],
  "URLDiario": "https://debates.parlamento.pt/catalogo/r3/dar/01/17/01/080/2026-04-18/57?pgs=57&org=PLC"
}
```

The current scraper does not import publications. The new importer should store them because they provide traceability for votes and discussions.

The newer samples also contain debate/intervention data under `IniEventos[].Intervencoesdebates[]`, especially on discussion/appreciation phases such as `190` and `243`. The important nested shape is:

```json
{
  "dataReuniaoPlenaria": "2026-01-28",
  "oradores": [
    {
      "deputadosOradores": [
        {
          "GP": "BE",
          "idCadastro": "7040",
          "nome": "Fabian Figueiredo"
        }
      ],
      "membrosGoverno": {
        "cargo": null,
        "governo": null,
        "nome": null
      },
      "horaInicio": "14:28",
      "horaTermo": "19:11",
      "linkVideo": [
        {
          "link": "https://av.parlamento.pt/videos/Plenary/17/1/51/67"
        }
      ],
      "publicacao": [
        {
          "pag": ["41-42"],
          "pubdt": "2026-01-29",
          "URLDiario": "https://debates.parlamento.pt/catalogo/..."
        }
      ],
      "sumario": "Procede a sexta alteracao ..."
    }
  ]
}
```

For `parlamento-app`, debate material should be stored but not forced into the swipe card. The product should show a short primary experience while offering official source links, debate transcript pages (`URLDiario`), and video links for users who want context.

## 4. Fields Extracted by the Current Scraper

`index.js::processProjetosLei` extracts:

```js
// index.js:253-281
const voteDate = votacaoGeneralObj["data"];
const legislatura = projetoLei["IniLeg"];
const sourceId = projetoLei["IniId"];
const grupo_parlamentar_proposal =
  projetoLei["IniAutorGruposParlamentares"][0]["GP"];
const proposalTitle = projetoLei["IniTitulo"];
const proposalLink = projetoLei["IniLinkTexto"];

let finalData = {
  voteDate: voteDate,
  legislatura: legislatura,
  sourceId: sourceId,
  proposingPartyAcronym: grupo_parlamentar_proposal,
  proposalTitle: proposalTitle,
  fullProposalTextLink: proposalLink,
  proposalTextHTML: proposalTextHTML,
  proposalResult: proposalResult,
  votingResultGenerality: generateVotingBlocks(votacaoGeneralObj),
  votingResultSpeciality: generateVotingBlocks(votacaoEspecialidadeObj),
};
```

Current extracted source-to-payload mapping:

| Source field | Current payload field | Notes |
| --- | --- | --- |
| `IniEventos` phase `250`, first `Votacao[0].data` | `voteDate` | Assumes a generality vote exists and has a first vote. |
| `IniLeg` | `legislatura` | Roman numeral string. |
| `IniId` | `sourceId` | Used for duplicate checks. |
| `IniAutorGruposParlamentares[0].GP` | `proposingPartyAcronym` | Only parliamentary group authors are supported. |
| `IniTitulo` | `proposalTitle` | No normalization except raw JSON parsing. |
| `IniLinkTexto` | `fullProposalTextLink` | Downloaded and also stored. |
| Downloaded/converted document | `proposalTextHTML` | HTML generated via `pdf2html`, then censored. |
| Vote results | `proposalResult` | Derived by `determineProposalResult`. |
| Phase `250` vote detail | `votingResultGenerality` | Derived by `generateVotingBlocks`. |
| Phase `320` vote detail | `votingResultSpeciality` | Despite the name, this is final/global vote in sample terminology. |

Fields present in the sample but not currently extracted:

- `IniNr`, `IniTipo`, `IniDescTipo`, `IniSel`
- `DataInicioleg`, `DataFimleg`
- `IniAutorOutros`, `IniAutorDeputados`
- `IniAnexos`
- all event metadata except votes
- phase attachments
- committee metadata and documents
- publications/debate links
- `TextosAprovados`
- related initiatives and interventions/debates
- raw source JSON

## 5. Current Mapping Into Backend/Database Models

The scraper does not write directly to a database. It maps Open Data JSON into an HTTP API payload and relies on the old API to persist it.

The implied old backend model from the POST body is:

```json
{
  "voteDate": "2026-04-17",
  "legislatura": "XVII",
  "sourceId": "356278",
  "proposingPartyAcronym": "PSD",
  "proposalTitle": "...",
  "fullProposalTextLink": "http://app.parlamento.pt/...",
  "proposalTextHTML": "<html-ish text>",
  "proposalResult": "ApprovedInSpeciality",
  "votingResultGenerality": {
    "isUninamous": false,
    "votingBlocks": [
      {
        "isUninamousWithinParty": true,
        "politicalPartyAcronym": "PSD",
        "votingOrientation": "InFavor"
      }
    ]
  },
  "votingResultSpeciality": {
    "isUninamous": true
  }
}
```

`update_live_database.js` confirms the old persisted shape expected a nested proposing party:

```js
// update_live_database.js:15-25
const finalData = {
  "voteDate": proposal["voteDate"],
  "legislatura": proposal["legislatura"],
  "sourceId": proposal["sourceId"],
  "proposingPartyAcronym": proposal["proposingParty"]["partyAcronym"],
  "proposalTitle": proposal["proposalTitle"],
  "fullProposalTextLink": proposal["fullProposalTextLink"],
  "proposalTextHTML" : proposal["proposalTextHTML"],
  "proposalResult" : proposal["proposalResult"],
  "votingResultGenerality" : proposal["votingResultGenerality"],
  "votingResultSpeciality" : proposal["votingResultSpeciality"],
}
```

For `parlamento-app`, the importer should map source data into domain entities directly rather than POSTing to the API.

Recommended domain vocabulary:

- `Initiative` or `ProjectLaw` for the initiative/proposal.
- `InitiativeAuthor` for parliamentary group, deputy, government, committee, or other author.
- `InitiativeEvent` for each `IniEventos[]` phase.
- `Vote` for each source `Votacao[]`.
- `VoteBlock` for parsed party-level vote orientations.
- `Document` for `IniLinkTexto`, `IniAnexos`, `AnexosFase`, and commission documents.
- `Publication` for `PublicacaoFase[]`.
- `DebateIntervention` and `DebateVideoLink` for `Intervencoesdebates[].oradores[]`.
- `InitiativeSummary` or `GeneratedSummary` for AI-generated summaries of redacted proposal text.
- `ImportRun`/`ImportError` for importer observability.

## 6. Transformations, Normalization, Filtering, and Assumptions

### Type filtering

Current filtering:

```js
iniciativa["IniDescTipo"] === "Projeto de Lei"
```

This excludes the provided sample (`"Proposta de Lei"`). The new importer should support at least:

- `Projeto de Lei`
- `Proposta de Lei`
- `Projeto de Resolução`

Prefer storing `IniDescTipo` as source data and making "importable types" configurable.

Product note: `Projeto de Resolução` is useful for the app, but it should be labelled as a resolution/recommendation. Do not merge it into a generic "law proposal" bucket in the UI.

### Vote-existence filtering

Current filtering requires at least one event with `CodigoFase === "250"`.

For this app, keep this filter. The user-facing product is built around voting on the introduced initiative in principle and comparing the user with Parliament's generality vote. If an initiative has no `250` generality phase, users cannot get the main comparison the app is designed around.

Importer behavior:

- Import only initiatives that have at least one `IniEventos[]` item with `CodigoFase === "250"` and a non-null/non-empty `Votacao[]`.
- Skip initiatives without a usable generality vote.
- Count skipped records in `import_runs.records_skipped`.
- Record a skip reason such as `MissingGeneralityVote` or `GeneralityPhaseWithoutVote`.
- Do not treat this as an error. It is a product eligibility filter.
- Keep this rule centralized/configurable so a future archival/admin importer can import non-voted initiatives if the product expands.

### Product voting frame

The intended app experience is "Tinder-like" voting on parliamentary initiatives. Based on the examples, the most honest default comparison point is the `250` generality vote:

- The generality vote asks whether Parliament supports the initiative in principle.
- Speciality/final text can differ from the introduced text.
- A final global vote (`320`) can refer to a combined final text involving multiple initiatives.
- Resolutions can be approved in generality but are not the same as laws.

The user-facing prompt should therefore be explicit, for example: "Would you support this initiative in principle, based on the introduced text?" Avoid implying the user is voting on the final law unless the card is specifically showing the final text.

Recommended reveal flow:

1. Show redacted/anonymized introduced text and AI summary.
2. Let the user vote/support/oppose/skip.
3. After the vote, reveal author party/deputies/government, official parliamentary vote, result, and source/debate links.

This keeps the first reaction less partisan while preserving accountability and context after the user has made their choice.

### Author filtering

Current filtering:

```js
// index.js:215-220
if (!projetoLei["IniAutorGruposParlamentares"]) {
  amountOfIndividualProposals++;
  console.log("This is an individual proposal. Skipping...");
  continue;
}
```

This assumption is wrong for the sample: government proposals are represented under `IniAutorOutros`.

New importer should support:

- `IniAutorGruposParlamentares[]`: parliamentary group authors.
- `IniAutorDeputados[]`: individual deputy authors.
- `IniAutorOutros`: government, committee, citizen group, or other source-defined author.

Do not skip solely because `IniAutorGruposParlamentares` is null.

### Vote result derivation

Current derivation:

```js
// index.js:17-32
function determineProposalResult(votacaoGeneralidadeObj, votacaoEspecialidadeObj) {
  let proposalResult = "ApprovedInGenerality";
  if (votacaoEspecialidadeObj) {
    if (votacaoEspecialidadeObj["resultado"] === "Rejeitdo") {
      proposalResult = "RejectedInSpeciality";
    } else {
      proposalResult = "ApprovedInSpeciality";
    }
  } else if (votacaoGeneralidadeObj["resultado"] === "Rejeitado") {
    proposalResult = "RejectedInGenerality";
  }

  return proposalResult;
}
```

Problems:

- `"Rejeitdo"` is misspelled, so rejected final/speciality votes will not be detected.
- Phase `"320"` is final global vote, not necessarily speciality vote.
- It assumes any non-rejected phase `320` means `ApprovedInSpeciality`.
- It cannot represent pending, withdrawn, expired, replaced, partially approved, or unknown states.

New result normalization should:

- Preserve the raw `resultado` string on each vote.
- Normalize known strings to an enum: `Approved`, `Rejected`, `Unknown`.
- Derive initiative status from the latest relevant vote/event, but keep the derivation separate from raw data.
- Use phase codes to derive stage: `Generality`, `Speciality`, `FinalGlobal`, `Committee`, `Other`.

### Vote detail parsing

Current parser:

```js
// index.js:144-189
function generateVotingBlocks(votacaoObj) {
  if (votacaoObj === null || Array.isArray(votacaoObj)) return null;
  if (votacaoObj["unanime"]) {
    return { isUninamous: true };
  }

  let votacaoObjString = votacaoObj["detalhe"];
  votacaoObjString = votacaoObjString.replaceAll("<BR>", "");
  votacaoObjString = votacaoObjString.replaceAll(":", "");
  votacaoObjString = votacaoObjString.replaceAll("-", "");
  votacaoObjString = votacaoObjString.replaceAll("<I>", "");
  votacaoObjString = votacaoObjString.replaceAll("</I>", "");

  const splits = splitBetweenKeywords(votacaoObjString);
  ...
  votingBlocks.push(...votingBlockByOrientation("InFavor", votacaoAFavor));
  votingBlocks.push(...votingBlockByOrientation("Against", votacaoContra));
  votingBlocks.push(...votingBlockByOrientation("Abstaining", votacaoAbstencao));
}
```

And party block parsing:

```js
// index.js:39-68
function votingBlockByOrientation(orientation, votacaoObj) {
  let nonUnanimousParties = [];
  let votingBlocks = [];
  for (let i = 0; i < votacaoObj.length; i++) {
    let votacaoIndividual = votacaoObj[i].replaceAll(" ", "");
    const politicalPartyAcronym = votacaoIndividual.replace(/[0-9]/g, "");
    if (politicalPartyAcronym.length > 7) continue;

    let votingBlock = {
      isUninamousWithinParty: true,
      politicalPartyAcronym: politicalPartyAcronym,
      votingOrientation: orientation,
    };

    if (nonUnanimousParties.includes(politicalPartyAcronym)) {
      votingBlock["isUninamousWithinParty"] = false;
    } else if (containsNumbers(votacaoIndividual)) {
      votingBlock["isUninamousWithinParty"] = false;
      nonUnanimousParties.push(politicalPartyAcronym);
      votingBlock["numberOfDeputies"] = parseInt(
        votacaoIndividual.replace(/[a-zA-Z]/g, "")
      );
    }
    votingBlocks.push(votingBlock);
  }
  return votingBlocks;
}
```

Current assumptions:

- Vote details are HTML-ish strings using `<BR>`, `<I>`, and `</I>`.
- Vote sections appear in order: `A Favor`, `Contra`, `Abstencao`.
- Party entries are comma-separated.
- Entries with numbers mean split/non-unanimous party vote counts, e.g. `77-PSD`.
- Entries with acronym length greater than 7 are deputy names and should be skipped.
- Absences are ignored.

New importer should preserve raw vote detail and parse best-effort blocks. The parser should:

- HTML-decode and strip tags using a real HTML parser or safe regex for simple tags.
- Recognize section labels with correct Portuguese accents and mojibake variants:
  - `A Favor`
  - `Contra`
  - `Abstencao` / `Abstenção` / current mojibake variant
  - `Ausencia` / `Ausência`
- Support both `77-PSD` and `PSD` entries.
- Store `NumberOfDeputies` when present.
- Mark `IsPartyUnanimous = false` when numeric counts appear or when the same party appears in multiple orientations.
- Store unparsed tokens in an error/details column instead of silently dropping them.

### PDF/document conversion and censorship

Current PDF support lives in [pdf_propostas.js](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/pdf_propostas.js).

Important functions:

- `convertToHTML(pdfPath)` calls `pdf2html.html(pdfPath)`.
- `loadForbiddenWords(legislature)` fetches `InformacaoBase{LEG}_json.txt`.
- `replaceHTMLSymbols(html)` uses [html_symbols.json](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/html_symbols.json).
- `spaceHTML(html)` inserts spaces around simple HTML tags.
- `removeForbiddenWords(text, forbidden_words)` censors party/deputy names and proposal heading text.
- `convertPDFtoHTML(pdfURL, outputFilename, forbidden_words)` downloads a document, writes a temporary file, converts it, transforms it, and returns HTML.

Snippet:

```js
// pdf_propostas.js:87-92
text = text.replace(/<head>[\s\S]*<\/head>/, "");
text = replaceHTMLSymbols(text);
text = spaceHTML(text);
text = removeForbiddenWords(text, forbidden_words);
```

Censorship:

```js
// pdf_propostas.js:58-67
const regex = new RegExp("[\n\b\r,.:; ]" + forbidden_words.join("[\n\b\r,:;. ]|[\n\b\r,;:. ]") + "[\n\b\r,:;. ]", "gi");
const text_without_forbidden_words = text.replace(regex, " <censored> ");

const regex_projeto_lei = new RegExp("<p> Projeto de Lei .*?</p>", "gis");
const text_without_projeto_lei = text_without_forbidden_words.replace(regex_projeto_lei, " <censored> ");
```

The censorship logic appears intended to remove names from the proposal text, probably to reduce model bias or hide party identity. In `parlamento-app`, this should become a first-class content pipeline:

- import official document metadata in the core JSON importer,
- fetch/extract document text in a separate document-content job,
- create a redacted text projection that removes party names, deputy names, and source author identifiers,
- optionally generate AI summaries from the redacted/extracted text when budget/configuration allows,
- store the model, prompt version, source document hash, and summary status for auditability.

Do not make the core initiative/vote import depend on document conversion or AI calls. The core record should be importable even if extraction or summarization fails.

### AI-generated summaries

AI summaries are useful for the product because the raw proposal PDFs/DOCX files are too long for a swipe card, but they must be optional, deferred, and parallelizable. The app should be able to import every initiative into PostgreSQL without spending AI credits. Summaries should run later against proposals that already exist in the database.

Treat AI summaries as generated aids rather than official sources. The summary worker can be disabled entirely, run only for the newest/current legislature, run only for proposals selected by an admin, or run in batches when credits are available.

Recommended rules:

- Do not call the AI summarization service inside the core Open Data import transaction.
- Queue summary work separately after the initiative and official text document already exist in the database.
- Make the summary worker horizontally parallelizable with row-level locking or `FOR UPDATE SKIP LOCKED`-style job claiming.
- Support priority and budget controls, e.g. summarize only proposals with upcoming/frontend visibility first.
- Generate summaries only from the official initiative text document, preferably after redaction.
- Store the official source text and/or extracted text separately from the generated summary.
- Store `model`, `prompt_version`, `input_document_hash`, `input_redaction_policy`, `generated_at`, and `generation_status`.
- Display summaries with a clear label such as "AI-generated summary from the official proposal text."
- Always provide a link to the official text and debate/publication pages.
- Regenerate summaries when the source document hash or redaction policy changes.
- Store summary warnings if extraction was partial, the document was malformed, or the model produced low-confidence output.
- Do not use AI summaries as the source of truth for legal status, vote result, authorship, or parliamentary outcome. Those must come from structured Open Data fields.

Ethically, the summary is acceptable if it reduces access friction while preserving auditability. It becomes misleading if it replaces official context, hides uncertainty, or makes users think the summary is neutral/complete.

### Encoding normalization

Many source strings in the current files and sample display mojibake (`VotaÃ§Ã£o`, `AbstenÃ§Ã£o`, `RepÃºblica`). The old code also searches for mojibake strings.

New importer should:

- Ensure HTTP responses are decoded as UTF-8.
- Use `System.Net.WebUtility.HtmlDecode` for HTML entities.
- Store raw source JSON for audit.
- Store normalized display text separately only if a reliable normalization step is implemented.
- Avoid business logic that depends only on mojibake spellings.

## 7. Duplicate Avoidance

Current duplicate avoidance is API-dependent:

```js
// index.js:204-210
const response = await fetch(
  `http://localhost:8080/proposal/source/${projetoLei["IniId"]}`
);
if (response.ok){
  console.log("This proposal is already stored in the database! SKIPPING");
  continue;
}
```

The POST also treats any non-OK response as "already stored":

```js
// index.js:294-296
if (!response.ok) {
  console.log("This proposal is already stored in the database!");
}
```

`update_live_database.js` retries non-404 failures forever, and treats `404` as duplicate:

```js
// update_live_database.js:45-53
if(!response.ok) {
  if(response.status == 404){
    console.log("This proposal is already stored in the database!")
  } else {
    await new Promise(r => setTimeout(r, 5000));
    makeRequest(finalData, sourceId);
  }
}
```

This is fragile and should not be migrated.

Recommended duplicate strategy in PostgreSQL:

- `initiatives.source_id` unique.
- `initiative_events` unique on `(initiative_id, source_event_id)` when `EvtId` exists, otherwise `(initiative_id, phase_code, phase_date, source_oev_id)`.
- `votes.source_vote_id` unique when `Votacao[].id` exists.
- `documents` unique on `(initiative_id, url)` or `(event_id, url)` depending owner.
- `publications` unique on `(event_id, url_diario, pub_date, page_range)`.
- `debate_interventions` should be deduped by `(initiative_id, event_id, speaker_source_id, start_time, end_time, summary hash)` when no source intervention ID exists.
- `debate_video_links` unique on `(debate_intervention_id, url)`.
- `generated_summaries` unique on `(initiative_id, document_id, summary_type, language, input_text_hash, prompt_version, model)` so summaries are regenerated only when inputs or generation policy change.
- Optional summary job rows should be deduped by `(initiative_id, document_id, summary_type, language, input_text_hash, prompt_version, model)` so parallel workers do not spend credits on the same generation.
- Use `INSERT ... ON CONFLICT DO UPDATE` via EF Core upsert pattern, raw SQL, or a small repository method.
- Store a hash of raw JSON per initiative (`source_hash`) to skip unchanged records and detect changes.

## 8. Reusable Parts

Reusable conceptually:

- The mapping from Open Data initiative fields to app proposal fields in `processProjetosLei`.
- Phase-code knowledge:
  - `250` = generality vote.
  - `320` = final global vote.
  - sample also shows `310` = speciality vote.
- Vote detail parsing idea from `generateVotingBlocks`, with a more robust parser.
- The use of `IniId`/`Votacao.id` as source identifiers for idempotency.
- Configured legislature source URL maps in `proposals_links.json` and base information URL maps in `forbidden_words.json`, preferably moved into appsettings/configuration.
- The idea of redacting names from generated text, if the product still needs a redacted text projection.

Reusable only after rewrite:

- `replaceHTMLSymbols`, because .NET has standard HTML decoding and the current dictionary appears to encode mojibake values.
- `removeForbiddenWords`, because its regex is brittle and can create false positives/negatives.
- PDF conversion, because `pdf2html` is a Node dependency and the target stack is ASP.NET Core.

## 9. Parts to Discard

Discard these implementation choices:

- HTTP self-posting from scraper to backend API. Use internal services/repositories.
- Hard-coded legislature `XIII`.
- Filtering only `Projeto de Lei`.
- Skipping all non-parliamentary-group authors.
- Treating missing/failed POST responses as duplicates.
- Infinite recursive retry in `update_live_database.js`.
- Temporary file naming like `download_${sourceId}.pdf` without isolated temp directories.
- Duplicated `convertPDFtoHTML` implementations in `index.js`, `pdf_propostas.js`, and `update_PDFs_already_in_db.js`.
- `get_json_struct.py`, which calls a live endpoint and is only an inspection helper.
- `test.js`, which only tests one regex.
- Current typo-sensitive result derivation (`"Rejeitdo"`).
- Current vote parser's silent dropping of tokens with acronym length greater than 7 as the only deputy-name guard.

## 10. Recommended ETL/Importer Redesign

Build the importer as an ASP.NET Core backend service with these layers:

### Source client

`IParliamentOpenDataClient`

Responsibilities:

- Fetch configured legislature initiative JSON.
- Fetch base information JSON if party/deputy metadata is needed.
- Optionally fetch documents later, not in the core initiative import.
- Use `HttpClientFactory`.
- Apply timeouts, retry with backoff, and response-size safeguards.
- Stream large JSON where possible.

For tests and local Codex work, implement `FileParliamentOpenDataClient` that reads `example_iniciativa.json`.

### DTO layer

Define DTOs matching source JSON names exactly, for example:

```csharp
public sealed class ParliamentInitiativeDto
{
    public string? IniId { get; init; }
    public string? IniLeg { get; init; }
    public string? IniNr { get; init; }
    public string? IniTipo { get; init; }
    public string? IniDescTipo { get; init; }
    public string? IniTitulo { get; init; }
    public string? IniLinkTexto { get; init; }
    public List<ParliamentEventDto>? IniEventos { get; init; }
    public List<ParliamentAttachmentDto>? IniAnexos { get; init; }
    public List<ParliamentGroupAuthorDto>? IniAutorGruposParlamentares { get; init; }
    public List<ParliamentDeputyAuthorDto>? IniAutorDeputados { get; init; }
    public ParliamentOtherAuthorDto? IniAutorOutros { get; init; }
}
```

Use `JsonPropertyName` if C# property names differ from source names. Keep DTOs separate from EF entities.

### Normalization/mapping layer

`IInitiativeMapper`

Responsibilities:

- Normalize source strings and dates.
- Map authors into a consistent author model.
- Map all events, votes, documents, and publications.
- Map debate interventions, transcript/publication links, and video links.
- Preserve raw source IDs.
- Compute derived status/result without discarding raw data.

### Vote parser

`IVoteDetailParser`

Input:

- source vote object (`id`, `resultado`, `unanime`, `detalhe`, `ausencias`)

Output:

- `Vote` entity with raw detail.
- zero or more `VoteBlock` entities.
- parse warnings.

### Persistence

Use one transaction per initiative or per batch chunk.

Preferred behavior:

- Upsert initiative.
- Upsert authors.
- Upsert events.
- Upsert votes and vote blocks.
- Upsert documents and publications.
- Upsert debate interventions and video links.
- Save raw JSON and source hash.
- Record parse/import warnings without failing the whole batch.

### Document content, redaction, and summary pipeline

This should be separate from the core Open Data JSON importer.

Recommended services:

- `IDocumentFetchService`: downloads official text documents using `IniLinkTexto`/document URLs.
- `IDocumentTextExtractor`: extracts plain text/HTML from PDF/DOCX.
- `IRedactionService`: removes or masks party names, deputy names, government author labels, and other configured identity markers.
- `IInitiativeSummaryService`: optionally generates a short user-facing summary from redacted extracted text.
- `IAiGenerationAuditRepository`: stores prompt/model/version/input hash/output/status.

The pipeline should be idempotent:

- If document URL and content hash are unchanged, skip extraction.
- If extracted text and redaction policy are unchanged, skip redaction.
- If AI summarization is disabled or out of budget, leave the proposal imported with official text/source links and no summary.
- If redacted text, prompt version, and model are unchanged, skip summary generation.
- If any step fails, keep the core initiative import successful and record the failure on the document/summary job.

### Import job orchestration

`ParliamentImportService`

Responsibilities:

- Start an `ImportRun`.
- Fetch source data.
- Iterate initiatives.
- Call mapper/parser.
- Persist idempotently.
- Track counts: read, inserted, updated, skipped, failed.
- Finish `ImportRun` with status.

## 11. Suggested PostgreSQL Tables and Fields

Names can be adapted to the existing `parlamento-app` conventions.

### `import_runs`

- `id uuid primary key`
- `source_name text not null`
- `legislature text null`
- `started_at timestamptz not null`
- `finished_at timestamptz null`
- `status text not null` (`Running`, `Succeeded`, `SucceededWithWarnings`, `Failed`, `Cancelled`)
- `records_read int not null default 0`
- `records_inserted int not null default 0`
- `records_updated int not null default 0`
- `records_skipped int not null default 0`
- `records_failed int not null default 0`
- `error_message text null`
- `created_by text null`

### `import_errors`

- `id uuid primary key`
- `import_run_id uuid references import_runs(id)`
- `source_id text null`
- `source_path text null`
- `severity text not null` (`Warning`, `Error`)
- `message text not null`
- `raw_fragment jsonb null`
- `created_at timestamptz not null`

### `import_skips`

Tracks product-eligibility skips that are not errors. For the current app, the main expected skip reason is an initiative missing a usable generality vote.

- `id uuid primary key`
- `import_run_id uuid references import_runs(id)`
- `source_id text null`
- `source_type text null` (`IniDescTipo`)
- `reason text not null` (`MissingGeneralityVote`, `GeneralityPhaseWithoutVote`, `UnsupportedType`, `DuplicateUnchanged`, `Other`)
- `message text null`
- `raw_fragment jsonb null`
- `created_at timestamptz not null`

### `initiatives`

- `id uuid primary key`
- `source_id text not null unique` (`IniId`)
- `legislature text not null` (`IniLeg`)
- `number text null` (`IniNr`)
- `type_code text null` (`IniTipo`)
- `type_description text null` (`IniDescTipo`)
- `selection text null` (`IniSel`)
- `title text not null` (`IniTitulo`)
- `epigraph text null` (`IniEpigrafe`)
- `observations text null` (`IniObs`)
- `text_substitution text null` (`IniTextoSubst`)
- `text_substitution_field text null` (`IniTextoSubstCampo`)
- `source_text_url text null` (`IniLinkTexto`)
- `legislature_start_date date null` (`DataInicioleg`)
- `legislature_end_date date null` (`DataFimleg`)
- `derived_status text not null default 'Unknown'`
- `derived_result text not null default 'Unknown'`
- `display_stage text not null default 'IntroducedText'` (`IntroducedText`, `Generality`, `FinalText`, `PublishedLaw`)
- `default_user_vote_stage text not null default 'Generality'`
- `generality_vote_date date null`
- `final_vote_date date null`
- `published_law_date date null`
- `published_law_text_id text null`
- `source_hash text not null`
- `raw_json jsonb not null`
- `first_imported_at timestamptz not null`
- `last_imported_at timestamptz not null`

### `initiative_authors`

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `author_type text not null` (`ParliamentaryGroup`, `Deputy`, `Government`, `Committee`, `Other`)
- `source_id text null`
- `name text null`
- `acronym text null`
- `party_acronym text null`
- `raw_json jsonb null`

Unique suggestion:

- `(initiative_id, author_type, coalesce(source_id,''), coalesce(acronym,''), coalesce(name,''))`

### `initiative_events`

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `source_event_id text null` (`EvtId`)
- `source_oev_id text null` (`OevId`)
- `source_text_id text null` (`OevTextId`)
- `phase_code text not null` (`CodigoFase`)
- `phase_name text null` (`Fase`)
- `phase_date date null` (`DataFase`)
- `observations text null` (`ObsFase`)
- `approved_text_id text null` (`TextosAprovados`)
- `raw_json jsonb null`

Unique suggestion:

- `(initiative_id, source_event_id)` where `source_event_id is not null`
- fallback unique `(initiative_id, phase_code, phase_date, source_oev_id)`

### `votes`

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `event_id uuid null references initiative_events(id) on delete cascade`
- `commission_id uuid null`
- `source_vote_id text not null`
- `vote_stage text not null` (`Generality`, `Speciality`, `FinalGlobal`, `Committee`, `Other`)
- `vote_date date null`
- `description text null`
- `raw_result text null`
- `normalized_result text not null` (`Approved`, `Rejected`, `Unknown`)
- `is_unanimous boolean not null default false`
- `meeting_number text null`
- `meeting_type text null`
- `raw_detail text null`
- `raw_absences jsonb null`
- `raw_json jsonb null`

Unique:

- `source_vote_id unique` if globally unique.
- If not globally unique, use `(initiative_id, source_vote_id)`.

### `vote_blocks`

- `id uuid primary key`
- `vote_id uuid not null references votes(id) on delete cascade`
- `party_acronym text not null`
- `orientation text not null` (`InFavor`, `Against`, `Abstaining`, `Absent`, `Unknown`)
- `number_of_deputies int null`
- `is_party_unanimous boolean not null default true`
- `raw_token text null`
- `parse_warning text null`

Unique suggestion:

- Do not over-constrain initially, because split votes can produce multiple rows for the same party across orientations.

### `documents`

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `event_id uuid null references initiative_events(id) on delete cascade`
- `commission_id uuid null`
- `document_scope text not null` (`InitiativeText`, `InitiativeAttachment`, `EventAttachment`, `CommissionDocument`)
- `document_type text null`
- `title text null`
- `url text not null`
- `document_date date null`
- `source_name text null`
- `content_html text null`
- `content_text text null`
- `redacted_content_text text null`
- `source_content_hash text null`
- `redacted_content_hash text null`
- `extraction_status text not null default 'NotStarted'` (`NotStarted`, `Succeeded`, `Failed`, `Skipped`)
- `redaction_status text not null default 'NotStarted'`
- `redaction_policy_version text null`
- `extracted_at timestamptz null`
- `redacted_at timestamptz null`
- `is_content_redacted boolean not null default false`
- `content_import_status text not null default 'NotImported'`
- `content_import_error text null`
- `raw_json jsonb null`

Unique:

- `(initiative_id, event_id, commission_id, document_scope, url)`

### `publications`

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `event_id uuid null references initiative_events(id) on delete cascade`
- `debate_intervention_id uuid null references debate_interventions(id) on delete cascade`
- `publication_date date null` (`pubdt`)
- `legislature text null` (`pubLeg`)
- `number text null` (`pubNr`)
- `series text null` (`pubSL`)
- `type text null` (`pubTipo`)
- `type_code text null` (`pubTp`)
- `pages text[] null` (`pag`)
- `url text null` (`URLDiario`)
- `raw_json jsonb null`

Use `event_id` for `PublicacaoFase[]` and `debate_intervention_id` for speaker-level `Intervencoesdebates[].oradores[].publicacao[]`.

### `debate_interventions`

Stores `IniEventos[].Intervencoesdebates[].oradores[]`. These rows let the frontend offer "learn more" context without putting the whole debate into the swipe card.

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `event_id uuid null references initiative_events(id) on delete cascade`
- `debate_date date null` (`dataReuniaoPlenaria`)
- `speaker_type text null` (`Deputy`, `GovernmentMember`, `Guest`, `Unknown`)
- `speaker_source_id text null` (`deputadosOradores[].idCadastro`)
- `speaker_name text null`
- `speaker_party_acronym text null` (`deputadosOradores[].GP`)
- `government_name text null` (`membrosGoverno.nome`)
- `government_role text null` (`membrosGoverno.cargo`)
- `session_phase text null` (`faseSessao`)
- `debate_phase text null` (`faseDebate`)
- `start_time text null` (`horaInicio`)
- `end_time text null` (`horaTermo`)
- `summary text null` (`sumario`)
- `raw_json jsonb null`

When mapping a debate intervention, also map:

- `linkVideo[]` into `debate_video_links`.
- `publicacao[]` into `publications` with `debate_intervention_id`.

### `debate_video_links`

- `id uuid primary key`
- `debate_intervention_id uuid not null references debate_interventions(id) on delete cascade`
- `url text not null`
- `raw_json jsonb null`

Unique:

- `(debate_intervention_id, url)`

### `generated_summaries`

Stores AI-generated summary artifacts for initiative cards. These are product aids, not official parliamentary facts.

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `document_id uuid null references documents(id) on delete set null`
- `summary_type text not null` (`SwipeCard`, `Detailed`, `NeutralBulletPoints`, `RiskCaveats`)
- `language text not null default 'pt-PT'`
- `summary_text text not null`
- `input_text_hash text not null`
- `input_document_hash text null`
- `redaction_policy_version text null`
- `prompt_version text not null`
- `model text not null`
- `provider text null`
- `generation_status text not null` (`Succeeded`, `Failed`, `Superseded`)
- `warnings text[] null`
- `error_message text null`
- `generated_at timestamptz not null`
- `superseded_at timestamptz null`

Unique:

- `(initiative_id, document_id, summary_type, language, input_text_hash, prompt_version, model)` for active/non-superseded records.

### `summary_jobs`

Optional queue table for deferred/parallel AI summary generation. This prevents the core importer from blocking on AI credits and lets multiple workers claim work safely.

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `document_id uuid not null references documents(id) on delete cascade`
- `summary_type text not null`
- `language text not null default 'pt-PT'`
- `input_text_hash text not null`
- `input_document_hash text null`
- `redaction_policy_version text null`
- `prompt_version text not null`
- `model text not null`
- `status text not null` (`Pending`, `Running`, `Succeeded`, `Failed`, `Skipped`, `Cancelled`)
- `priority int not null default 0`
- `attempt_count int not null default 0`
- `max_attempts int not null default 3`
- `not_before timestamptz null`
- `claimed_by text null`
- `claimed_at timestamptz null`
- `last_error text null`
- `created_at timestamptz not null`
- `updated_at timestamptz not null`

Unique:

- `(initiative_id, document_id, summary_type, language, input_text_hash, prompt_version, model)`

Workers should claim pending rows in small batches ordered by `priority desc, created_at asc`, using a database-level lock/claim pattern so several worker instances can run in parallel without generating the same summary twice.

### `commissions`

If `parlamento-app` already has a commission table, map into it. Otherwise:

- `id uuid primary key`
- `source_commission_id text null` (`IdComissao`)
- `number text null` (`Numero`)
- `name text not null`

### `initiative_commissions`

- `id uuid primary key`
- `initiative_id uuid not null references initiatives(id) on delete cascade`
- `event_id uuid null references initiative_events(id) on delete cascade`
- `commission_id uuid not null references commissions(id)`
- `source_accession_id text null` (`AccId`)
- `is_competent boolean null` (`Competente == "S"`)
- `distribution_date date null` (`DataDistribuicao`)
- `raw_json jsonb null`

## 12. Suggested ASP.NET Core Background Job Design

Use one of these approaches:

### Simple built-in option

- `BackgroundService` plus a queue table or `Channel<ImportRequest>`.
- Admin/API endpoint enqueues imports:
  - `POST /admin/imports/parliament?legislature=XVII`
  - `POST /admin/imports/parliament/from-file` for fixtures/tests.
- Service processes one legislature at a time to avoid duplicate writes.
- Use PostgreSQL advisory lock or an `import_locks` table to prevent concurrent imports for the same legislature.

### Production scheduler option

- Hangfire or Quartz.NET for recurring jobs.
- Schedule nightly import for current legislature.
- Manual jobs for historical legislatures.
- Store run status in `import_runs`.

Recommended service classes:

```text
Infrastructure/OpenData/ParliamentOpenDataClient.cs
Infrastructure/OpenData/FileParliamentOpenDataClient.cs
Application/Imports/ParliamentImportService.cs
Application/Imports/InitiativeMapper.cs
Application/Imports/VoteDetailParser.cs
Application/Imports/ImportRunRepository.cs
Application/Imports/ParliamentImportWorker.cs
Application/Imports/DebateMapper.cs
Application/Documents/DocumentContentImportWorker.cs
Application/Documents/DocumentTextExtractor.cs
Application/Documents/RedactionService.cs
Application/Summaries/InitiativeSummaryWorker.cs
Application/Summaries/InitiativeSummaryService.cs
```

Operational requirements:

- `HttpClient.Timeout` appropriate for large files.
- `CancellationToken` honored throughout.
- Backoff/retry only for transient network errors.
- No infinite recursion.
- Batch progress committed periodically.
- Raw JSON retained for audit and reprocessing.
- Document content extraction executed as a separate job after the core JSON import.
- AI summary generation is optional and executed only when enabled/budgeted after extraction and redaction succeed.
- Debate/publication/video links imported in the core JSON import, because they are structured source metadata and do not require expensive document processing.

Suggested job chain:

1. `ParliamentImportWorker`: imports structured initiative/event/vote/document/debate metadata.
2. `DocumentContentImportWorker`: downloads and extracts official initiative text for records that need content.
3. `RedactionWorker` or `RedactionService`: produces redacted text from extracted official text.
4. Optional `SummaryJobSeeder`: creates `summary_jobs` for eligible proposals when summarization is enabled, credits are available, or an admin explicitly requests it.
5. Optional `InitiativeSummaryWorker`: claims pending `summary_jobs` in parallel and generates/refreshes AI summaries when redacted text, prompt version, or model changes.

## 13. Pseudocode for New Importer

### Import orchestration

```csharp
public async Task<ImportRunResult> ImportLegislatureAsync(
    string legislature,
    CancellationToken cancellationToken)
{
    await using var importLock = await importLockProvider.TryAcquireAsync(
        $"parliament-import:{legislature}",
        cancellationToken);

    if (importLock is null)
        return ImportRunResult.Skipped("Import already running.");

    var run = await importRuns.StartAsync("PortugueseParliamentOpenData", legislature, cancellationToken);

    try
    {
        await foreach (var dto in sourceClient.GetInitiativesAsync(legislature, cancellationToken))
        {
            run.RecordsRead++;

            try
            {
                var rawJson = rawJsonSerializer.Serialize(dto);
                var hash = hashService.Sha256(rawJson);

                var generalityVote = VoteSelector.FindUsableGeneralityVote(dto);
                if (generalityVote is null)
                {
                    run.RecordsSkipped++;
                    await importSkips.AddAsync(
                        run.Id,
                        dto.IniId,
                        dto.IniDescTipo,
                        reason: SkipReason.MissingGeneralityVote,
                        message: "Initiative has no phase 250 event with a usable Votacao array.",
                        rawFragment: dto,
                        cancellationToken);
                    continue;
                }

                var existing = await db.Initiatives
                    .SingleOrDefaultAsync(x => x.SourceId == dto.IniId, cancellationToken);

                if (existing is not null && existing.SourceHash == hash)
                {
                    run.RecordsSkipped++;
                    continue;
                }

                var mapped = mapper.Map(dto, rawJson, hash);

                await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                await initiativeRepository.UpsertGraphAsync(mapped, cancellationToken);

                await tx.CommitAsync(cancellationToken);

                if (existing is null) run.RecordsInserted++;
                else run.RecordsUpdated++;
            }
            catch (Exception ex)
            {
                run.RecordsFailed++;
                await importErrors.AddAsync(run.Id, dto.IniId, "Error", ex.Message, dto, cancellationToken);
            }

            await importRuns.UpdateProgressAsync(run, cancellationToken);
        }

        await importRuns.SucceedAsync(run, cancellationToken);
        return ImportRunResult.Success(run.Id);
    }
    catch (Exception ex)
    {
        await importRuns.FailAsync(run, ex, cancellationToken);
        throw;
    }
}
```

### Mapping one initiative

```csharp
public InitiativeGraph Map(ParliamentInitiativeDto dto, string rawJson, string sourceHash)
{
    var initiative = new Initiative
    {
        SourceId = Required(dto.IniId, "IniId"),
        Legislature = Required(dto.IniLeg, "IniLeg"),
        Number = dto.IniNr,
        TypeCode = dto.IniTipo,
        TypeDescription = dto.IniDescTipo,
        Selection = dto.IniSel,
        Title = Required(dto.IniTitulo, "IniTitulo"),
        SourceTextUrl = dto.IniLinkTexto,
        LegislatureStartDate = ParseDate(dto.DataInicioleg),
        LegislatureEndDate = ParseDate(dto.DataFimleg),
        RawJson = rawJson,
        SourceHash = sourceHash
    };

    AddAuthors(initiative, dto);
    AddPrimaryDocument(initiative, dto.IniLinkTexto);
    AddAttachments(initiative, dto.IniAnexos, DocumentScope.InitiativeAttachment);

    foreach (var eventDto in dto.IniEventos ?? [])
    {
        var evt = MapEvent(eventDto);
        initiative.Events.Add(evt);

        AddAttachments(initiative, evt, eventDto.AnexosFase);
        AddPublications(initiative, evt, eventDto.PublicacaoFase);
        AddDebateInterventions(initiative, evt, eventDto.Intervencoesdebates);
        AddVotes(initiative, evt, eventDto.Votacao, VoteStageFromPhase(eventDto.CodigoFase));
        AddCommissions(initiative, evt, eventDto.Comissao);
    }

    initiative.DerivedResult = DeriveResult(initiative.Votes);
    initiative.DerivedStatus = DeriveStatus(initiative.Events, initiative.Votes);

    return new InitiativeGraph(initiative);
}
```

### Product eligibility: usable generality vote

```csharp
public static ParliamentVoteDto? FindUsableGeneralityVote(ParliamentInitiativeDto dto)
{
    var generalityEvents = (dto.IniEventos ?? [])
        .Where(e => e.CodigoFase == "250")
        .OrderBy(e => ParseDate(e.DataFase))
        .ToList();

    var latestWithVote = generalityEvents
        .LastOrDefault(e => e.Votacao is { Count: > 0 });

    return latestWithVote?.Votacao?.FirstOrDefault();
}
```

Current product rule: if this returns null, skip the initiative and record an `import_skips` row. This is not a data-quality failure; it means the initiative is not eligible for the current swipe/vote experience.

### Vote parsing

```csharp
public ParsedVoteDetail Parse(ParliamentVoteDto voteDto)
{
    if (IsTruthyUnanimous(voteDto.Unanime))
    {
        return ParsedVoteDetail.Unanimous();
    }

    if (string.IsNullOrWhiteSpace(voteDto.Detalhe))
    {
        return ParsedVoteDetail.Empty("Vote detail is empty and vote is not marked unanimous.");
    }

    var text = WebUtility.HtmlDecode(voteDto.Detalhe);
    text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"</?i>", "", RegexOptions.IgnoreCase);

    var sections = SplitVoteSections(text);
    var blocks = new List<VoteBlock>();

    foreach (var section in sections)
    {
        foreach (var token in section.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Regex.Match(token, @"^(?:(?<count>\d+)\s*-?\s*)?(?<party>[A-Z][A-Z0-9-]{0,10})$");

            if (!match.Success)
            {
                blocks.Add(VoteBlock.Unparsed(section.Orientation, token, "Could not parse party/count token."));
                continue;
            }

            blocks.Add(new VoteBlock
            {
                Orientation = section.Orientation,
                PartyAcronym = match.Groups["party"].Value,
                NumberOfDeputies = ParseNullableInt(match.Groups["count"].Value),
                IsPartyUnanimous = !match.Groups["count"].Success,
                RawToken = token
            });
        }
    }

    MarkPartiesAppearingInMultipleOrientationsAsNonUnanimous(blocks);
    return ParsedVoteDetail.FromBlocks(blocks);
}
```

### Stage derivation

```csharp
private static VoteStage VoteStageFromPhase(string? phaseCode) => phaseCode switch
{
    "250" => VoteStage.Generality,
    "310" => VoteStage.Speciality,
    "320" => VoteStage.FinalGlobal,
    "370" => VoteStage.PostApproval,
    "380" => VoteStage.PostApproval,
    "390" => VoteStage.PostApproval,
    "400" => VoteStage.PostApproval,
    "580" => VoteStage.PublishedLaw,
    _ => VoteStage.Other
};

private static NormalizedVoteResult NormalizeResult(string? raw) =>
    NormalizeText(raw) switch
    {
        "aprovado" => NormalizedVoteResult.Approved,
        "aprovada" => NormalizedVoteResult.Approved,
        "rejeitado" => NormalizedVoteResult.Rejected,
        "rejeitada" => NormalizedVoteResult.Rejected,
        _ => NormalizedVoteResult.Unknown
    };
```

### Document extraction, redaction, and summary generation

```csharp
public async Task ProcessDocumentContentAsync(Guid documentId, CancellationToken cancellationToken)
{
    var document = await db.Documents
        .Include(x => x.Initiative)
        .SingleAsync(x => x.Id == documentId, cancellationToken);

    if (document.DocumentScope != DocumentScope.InitiativeText)
        return;

    var downloaded = await documentFetchService.DownloadAsync(document.Url, cancellationToken);
    var contentHash = hashService.Sha256(downloaded.Bytes);

    if (document.SourceContentHash == contentHash &&
        document.ExtractionStatus == "Succeeded")
    {
        return;
    }

    var extracted = await documentTextExtractor.ExtractAsync(downloaded, cancellationToken);

    document.ContentText = extracted.PlainText;
    document.ContentHtml = extracted.Html;
    document.SourceContentHash = contentHash;
    document.ExtractionStatus = "Succeeded";
    document.ExtractedAt = clock.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    await redactionQueue.EnqueueAsync(document.Id, cancellationToken);
}

public async Task RedactDocumentAsync(Guid documentId, CancellationToken cancellationToken)
{
    var document = await db.Documents
        .Include(x => x.Initiative)
        .ThenInclude(x => x.Authors)
        .SingleAsync(x => x.Id == documentId, cancellationToken);

    var redaction = redactionService.Redact(
        document.ContentText,
        new RedactionContext
        {
            Legislature = document.Initiative.Legislature,
            Authors = document.Initiative.Authors,
            PolicyVersion = RedactionPolicy.CurrentVersion
        });

    document.RedactedContentText = redaction.Text;
    document.RedactedContentHash = hashService.Sha256(redaction.Text);
    document.RedactionPolicyVersion = redaction.PolicyVersion;
    document.RedactionStatus = "Succeeded";
    document.RedactedAt = clock.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    if (!summaryOptions.Enabled)
        return;

    if (!summaryBudget.CanQueueMoreWork())
        return;

    await summaryJobs.EnqueueIfMissingAsync(new SummaryJobRequest
    {
        InitiativeId = document.InitiativeId,
        DocumentId = document.Id,
        SummaryType = SummaryType.SwipeCard,
        Language = "pt-PT",
        InputTextHash = document.RedactedContentHash,
        InputDocumentHash = document.SourceContentHash,
        RedactionPolicyVersion = document.RedactionPolicyVersion,
        PromptVersion = PromptVersions.SwipeCardV1,
        Model = Models.DefaultSummaryModel,
        Priority = SummaryPriority.For(document.Initiative)
    }, cancellationToken);
}

public async Task RunSummaryWorkerAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var jobs = await summaryJobs.ClaimPendingBatchAsync(
            workerId: workerIdentity.Name,
            batchSize: summaryOptions.BatchSize,
            cancellationToken);

        if (jobs.Count == 0)
            break;

        await Parallel.ForEachAsync(
            jobs,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = summaryOptions.MaxParallelism,
                CancellationToken = cancellationToken
            },
            async (job, ct) => await GenerateSummaryForJobAsync(job, ct));
    }
}

private async Task GenerateSummaryForJobAsync(SummaryJob job, CancellationToken cancellationToken)
{
    if (!summaryBudget.TryReserveCredit())
    {
        await summaryJobs.ReleaseAsync(job.Id, notBefore: clock.UtcNow.AddHours(6), cancellationToken);
        return;
    }

    var existing = await summaries.FindActiveAsync(
        job.InitiativeId,
        job.DocumentId,
        job.SummaryType,
        job.InputTextHash,
        job.PromptVersion,
        job.Model,
        cancellationToken);

    if (existing is not null)
    {
        await summaryJobs.MarkSkippedAsync(job.Id, "Summary already exists.", cancellationToken);
        return;
    }

    var document = await db.Documents.SingleAsync(x => x.Id == job.DocumentId, cancellationToken);

    var summary = await initiativeSummaryService.GenerateAsync(
        redactedOfficialText: document.RedactedContentText,
        promptVersion: job.PromptVersion,
        cancellationToken);

    db.GeneratedSummaries.Add(new GeneratedSummary
    {
        InitiativeId = job.InitiativeId,
        DocumentId = job.DocumentId,
        SummaryType = job.SummaryType,
        Language = job.Language,
        SummaryText = summary.Text,
        InputTextHash = job.InputTextHash,
        InputDocumentHash = job.InputDocumentHash,
        RedactionPolicyVersion = job.RedactionPolicyVersion,
        PromptVersion = job.PromptVersion,
        Model = summary.Model,
        Provider = summary.Provider,
        GenerationStatus = "Succeeded",
        Warnings = summary.Warnings,
        GeneratedAt = clock.UtcNow
    });

    await db.SaveChangesAsync(cancellationToken);
    await summaryJobs.MarkSucceededAsync(job.Id, cancellationToken);
}
```

## 14. Edge Cases and Failure Modes

### Source shape and size

- Real initiative files are large. Use streaming deserialization where possible.
- The sample is a single object, while production files are expected arrays. Support both.
- Some arrays may be null, empty, or omitted.
- IDs are strings in the sample even when numeric-looking. Store source IDs as text.

### Encoding

- Mojibake appears in local files and sample output.
- Do not key business logic solely on garbled labels.
- Normalize accents for comparisons using a helper that lowercases, trims, decodes HTML, and optionally strips diacritics.

### Missing votes

- Current code crashes if `votacaoGeneralObj` is null and then reads `votacaoGeneralObj["data"]`.
- Some initiatives may be pending and have no phase `250`.
- Some events may have `CodigoFase` but no `Votacao`.
- Some vote arrays may contain multiple votes; current code only uses `[0]`.

Current product behavior: skip initiatives that do not have a usable phase `250` generality vote, record an `import_skips` row, and continue. This keeps the app dataset aligned with the swipe flow. A future archival importer can relax this rule if the app later needs non-voted/pending initiatives.

### Multiple events for same phase

The sample has two `"270"` events. Current code takes the last event for phases it cares about. New importer should store all events and choose derived status based on dates/stages.

### Initiative lifecycle and final text

- `Projeto de Lei` can continue after final global vote through decree, promulgation, referenda, INCM dispatch, and law publication (`580`).
- `Projeto de Resolução` can have a generality vote and debate but should not be labelled as a law.
- Final global vote descriptions can refer to final texts combining multiple initiatives.
- User votes collected against the redacted introduced text should be compared primarily to the generality vote, not blindly to final law publication.
- Store `TextosAprovados` and `OevTextId` because approved/decree/published text can differ from the introduced proposal.

### Vote parsing

- `detalhe` can be null for unanimous votes.
- `unanime` is a string (`"unanime"`) rather than boolean.
- Vote detail can include absences.
- Vote detail can include party counts (`77-PSD`) or simple acronyms (`PSD`).
- Party acronyms can contain hyphens (`CDS-PP`).
- Deputy names may appear in detail strings.
- Parties may appear in multiple orientations when split.
- Current parser removes hyphens globally; new parser should not do that before token parsing because acronyms can contain hyphens.

### Authors

- `IniAutorGruposParlamentares` can be null.
- `IniAutorDeputados` can be null.
- `IniAutorOutros` can contain government authors.
- Multiple parliamentary groups/deputies may author one initiative.

### Documents

- `IniLinkTexto` in the sample points to a `.docx` filename through a `doc.pdf` endpoint. Do not assume extension or MIME type from URL path.
- Document downloads should be separate from core JSON import.
- Temporary files must use safe unique paths.
- Conversion failures should not fail initiative import.
- Redaction can remove meaningful context if it is too broad. Redact author/party/deputy identity, but avoid blindly deleting institutions, laws, regions, or entities that are substantively part of the proposal.
- AI summaries must be invalidated when document content, redaction policy, prompt version, or model changes.
- Summary generation failures should not block the official initiative record from being visible; show source links and a "summary unavailable" state.

### Debate links and publications

- Debate `sumario` fields can mention several initiatives discussed together, not only the current initiative.
- `Intervencoesdebates[].oradores[]` can include deputies, government members, guests, video links, and publication slices.
- `PublicacaoFase[].URLDiario` and speaker-level `publicacao[].URLDiario` are official traceability links and should be preserved even if the app card does not display them by default.
- Video links may disappear or change availability; store the URL and import timestamp, but do not treat video availability as required for core import success.

### Duplicates and updates

- Source files may change existing initiatives/events/votes.
- Unique source IDs plus source hashes should avoid duplicate rows and unnecessary updates.
- Upserts should be transactional per initiative.
- Raw JSON should be retained for replay/debugging.

### Network and scheduling

- Do not retry forever.
- Use bounded retries with jitter.
- Record failed source downloads in `import_runs`.
- Prevent concurrent imports for the same legislature.
- Allow cancellation.

### Current-code bugs to avoid

- `determineProposalResult` checks `"Rejeitdo"` instead of `"Rejeitado"`.
- `generateVotingBlocks` returns null when passed an array, but the source `Votacao` property is normally an array; callers pass `[0]` manually.
- `generateVotingBlocks` ignores `Ausencia`.
- `votingBlockByOrientation` can throw if passed `undefined`; current split paths usually avoid this but it is not defensive.
- Current duplicate handling confuses API status codes.
- `updateProposal` uses `proposalObj.id` as `sourceId` in [update_PDFs_already_in_db.js](C:/Users/Poodlers/OneDrive/Desktop/Pwogwamin/parlamento-scraper/update_PDFs_already_in_db.js), which may be the database ID rather than source `IniId`.

## Implementation Handoff Summary

The useful migration target is not the Node scripts themselves but the source contract and mapping rules:

- Source initiatives are keyed by `IniId`.
- Events are under `IniEventos`.
- Vote events are under `IniEventos[].Votacao[]`.
- The current app importer should skip initiatives without a usable phase `250` generality vote and record the skip reason.
- Phase `250` is generality, `310` is speciality, and `320` is final global.
- Vote `detalhe` can be parsed into party-level blocks, but raw vote detail must always be stored.
- The importer must support `Projeto de Lei` and `Proposta de Lei`, parliamentary group authors, deputy authors, and government/other authors.
- The importer must also support `Projeto de Resolução` and clearly distinguish it from lawmaking initiatives.
- Debate transcript links (`URLDiario`) and video links under `Intervencoesdebates` must be imported as core traceability metadata.
- AI summaries are part of the product pipeline, but they must be generated from extracted/redacted official text and stored with model/prompt/input-hash audit metadata.
- User vote comparison should default to the `250` generality vote when the frontend shows the introduced proposal text.
- PostgreSQL uniqueness and upserts should replace the old duplicate-check HTTP calls.
- Document extraction/redaction/summary generation should be a separate pipeline after core JSON import; extraction/redaction are useful prerequisites for the card experience, while AI summary generation is a first-class optional capability that can be budgeted, delayed, parallelized, or disabled.
