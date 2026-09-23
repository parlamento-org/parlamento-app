# Backend API

ASP.NET Core Web API for the Parlamento app.

## Current structure

- `src/`: API layer (`Program.cs`, controllers, Swagger, HTTP-only concerns)
- `Application/`: request DTOs, validators, service contracts
- `Domain/`: entities and enums
- `Infrastructure/`: `DbContext`, service implementations, PostgreSQL migration baseline
- `test/`: unit-test project
- `integration-tests/`: integration-test project

## Database

Primary database: PostgreSQL via `ConnectionStrings__DefaultConnection`.

There is still a SQLite fallback in code for local compatibility, but Docker now targets PostgreSQL and no longer depends on `db.sqlite`.

Configuration is loaded by ASP.NET Core from `appsettings.json`, environment-specific appsettings files, environment variables, and command-line arguments. Environment variables override appsettings values. Nested keys use double underscores, for example `ParliamentOpenData__LatestLegislature`.

### Example connection string

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=parlamento;Username=parlamento;Password=parlamento"
```

## Docker

Start the development stack:

```powershell
docker compose up postgres-parlamento db-parlamento-migrate dev-parlamento-be dev-parlamento-fe
```

To run the development backend against Neon instead, set `PARLAMENTO_NEON_CONNECTION_STRING` in the root `.env` file, then use the Neon profile:

```powershell
docker compose --profile neon up db-parlamento-migrate-neon dev-parlamento-be-neon dev-parlamento-fe
```

Do not commit the Neon connection string. The root `.env` file is ignored; `.env.example` documents the expected keys without secrets.

Start only the backend side:

```powershell
docker compose up postgres-parlamento db-parlamento-migrate dev-parlamento-be
```

Production-style backend container:

```powershell
docker compose up postgres-parlamento db-parlamento-migrate prod-parlamento-be
```

## Migrations

Apply migrations through the dedicated migration container:

```powershell
docker compose up db-parlamento-migrate
```

If migration files changed, rebuild the migration image before running it:

```powershell
docker compose build --no-cache db-parlamento-migrate
docker compose up db-parlamento-migrate
```

The repository currently contains:

- legacy SQLite migrations in `src/Migrations/`
- PostgreSQL migrations in `Infrastructure/Migrations/`

The importer schema stores compact source metadata and parsed source traceability rows. It keeps `ProjectLaw.SourceHash` for update detection, but does not persist the full raw Open Data JSON payload.

## Parliament Open Data Import

The backend can import Portuguese Parliament Open Data initiatives into PostgreSQL. The importer:

- reads local JSON files for development and tests
- downloads configured legislature feeds in production
- supports both single-object and array-root JSON feeds
- streams array feeds instead of loading the full legislature file into memory
- skips initiatives without a usable phase `250` generality vote
- upserts initiatives by source id and source hash
- records import runs, skips, and per-record errors
- stores core proposal fields on `ProjectLaw`
- stores source traceability in imported author, event, vote, vote block, document, publication, and intervention tables

`VotingResult` and `VotingBlock` are the older app-facing vote summary tables used by the current proposal/feed flow. `ParliamentInitiativeVote` and `ParliamentInitiativeVoteBlock` store all official source vote events and parsed party blocks for traceability.

### Base information and redaction

The redaction pipeline uses Parliament base-information JSON files to load deputy names for each legislature. The base-info importer stores:

- deputies in `ParliamentDeputies`
- legislature-scoped parliamentary groups in `ParliamentaryGroups`
- compact redaction terms in `ParliamentRedactionTerms`

Redaction terms include deputy full names, deputy parliamentary names, parliamentary group acronyms, parliamentary group names, party acronyms, party names, initiative author names/acronyms, and accentless variants of names that contain diacritics. Matching is case-insensitive, so names like `PARTIDO COMUNISTA PORTUGUES` are redacted from terms loaded as `Partido Comunista Português`.

The document processor then fetches only the initiative text document referenced by `ProjectLaw.FullProposalTextLink`, extracts it into an internal structured document model, redacts that model, and stores only redacted output plus hashes/status in `ParliamentDocumentContents`.

The database does not store the original extracted document text. It stores:

- source content hash and size
- redacted structured document model JSON
- redacted text for search, indexing, AI summarisation, and future NLP
- redacted HTML rendered from the structured model for frontend presentation
- extraction/redaction status and error message
- extractor kind/version, renderer version, document model schema version, and redaction policy version

The document pipeline is intentionally independent from the Open Data initiative importer:

```text
PDF/DOCX/HTML/TXT
  -> DocumentModel
  -> model-aware redaction
  -> structured HTML
  -> Flutter presentation

DocumentModel
  -> plain text
  -> search, indexing, AI summaries
```

Redactions are represented as semantic model runs and rendered as inline HTML spans, for example:

```html
<span class="redacted" data-length="18" style="--redaction-width:9.4em"></span>
```

The frontend should render `.redacted` as a solid black inline block using `--redaction-width`, so surrounding text does not collapse.

Current extractors:

- `.pdf` via open-source `iText` for selectable-text PDFs
- `.pdf` via open-source `PdfPig` as a retained diagnostic/fallback extractor
- `.txt`
- `.html` / `.htm`
- `.docx`

PDF extraction is heuristic and layout-aware, not pixel-perfect. The production PDF extractor now uses iText because comparison against Parliament samples showed it preserves normal Portuguese word spacing, quote punctuation, italic words, and party names much more reliably than the current PdfPig glyph reconstruction. PdfPig remains available for comparison and regression work. Scanned-image PDFs still fail cleanly because they require a future OCR pipeline.

### Import configuration

Open Data URLs are configured under `ParliamentOpenData`. `appsettings.json` contains defaults for legislatures `XV`, `XVI`, and `XVII`.

For Docker Compose, the root `.env` file provides substitution values, and `docker-compose.yml` passes them into backend containers as ASP.NET environment variables. The default backend service uses the local `postgres-parlamento` container; the `neon` Compose profile uses `PARLAMENTO_NEON_CONNECTION_STRING` from `.env`.

Important keys:

```text
ParliamentOpenData__LatestLegislature=XVII
ParliamentOpenData__DailyImport__Enabled=false
ParliamentOpenData__DailyImport__RunSummaries=true
ParliamentOpenData__DailyImport__TimeZoneId=Europe/Lisbon
ParliamentOpenData__DailyImport__RunAt=00:00:00
ParliamentOpenData__Legislatures__XV__InitiativesUrl=...
ParliamentOpenData__Legislatures__XV__BaseInfoUrl=...
ParliamentOpenData__Legislatures__XVI__InitiativesUrl=...
ParliamentOpenData__Legislatures__XVI__BaseInfoUrl=...
ParliamentOpenData__Legislatures__XVII__InitiativesUrl=...
ParliamentOpenData__Legislatures__XVII__BaseInfoUrl=...
```

`appsettings.json` works in production if it is copied into the deployed app, but environment variables are preferred for deployments because they can vary per environment and override image-baked defaults without rebuilding the image.

### Command mode

The API executable also supports one-shot import commands. Command mode builds the normal dependency injection container, runs the import, logs the result, and exits without starting HTTP listeners.

Import a local sample file:

```powershell
dotnet run --project backend/src -- parliament-import --file docs/samples/example_iniciativa.json
```

Import configured legislatures:

```powershell
dotnet run --project backend/src -- parliament-import --legislatures XVII
dotnet run --project backend/src -- parliament-import --legislatures XVII,XVI,XV
dotnet run --project backend/src -- parliament-import --legislatures XVII XVI XV
```

If no legislature is supplied, command mode falls back to `ParliamentOpenData:LatestLegislature`.

Seed the database end-to-end:

```powershell
dotnet run --project backend/src -- parliament-seed
dotnet run --project backend/src -- parliament-seed --legislatures XVII
dotnet run --project backend/src -- parliament-seed --legislatures XVII,XVI,XV
dotnet run --project backend/src -- parliament-seed --no-summary
dotnet run --project backend/src -- parliament-seed --max-documents 25
dotnet run --project backend/src -- parliament-seed --force-topic-assignments
```

`parliament-seed` runs the full backend data pipeline: Open Data import, base-info preflight, document extraction/redaction for `ProjectLaw.FullProposalTextLink`, reviewed proposal topic taxonomy import, historical topic assignment seed import, automatic topic assignment for newly redacted documents when embeddings are configured, then AI summaries from the stored redacted plain text. If no legislature is supplied, it processes every configured legislature under `ParliamentOpenData:Legislatures`. Each phase remains idempotent: unchanged Open Data rows are skipped by source hash, current redacted documents are skipped by source/extractor/redaction/renderer versions, reviewed topic assignments are preserved, automatic topic assignments are skipped when current for the redacted source hash, and current summaries are skipped by source hash/model/prompt version. Use `--no-summary` to stop before summaries, `--force-import` to rebuild existing imported initiative rows, `--force-redaction` to rewrite document content, `--force-summary` to regenerate summaries, `--force-topic-assignments` to regenerate automatic topic assignments that are not reviewed/seeded, or `--force` for all forced phases.

Open Data refreshes preserve the existing primary `InitiativeText` document row for each proposal, so a changed initiative payload does not cascade-delete the last successful redaction or summary. If a current document URL returns the Parliament unavailable-resource message instead of proposal content, redaction records a failed refresh but keeps the previous successful redacted text, hash, and status available to the app.

The topic taxonomy import runs even when OpenAI is not configured. Automatic topic assignment requires `OPENAI_API_KEY` or `ProposalTopics:ApiKey`; when neither is present, `parliament-seed` logs and prints that automatic topic assignment was skipped.

Seed only the reviewed topic taxonomy and historical reviewed assignments from the bundled taxonomy artifacts:

```powershell
dotnet run --project backend/src -- parliament-topics seed-reviewed
```

This command imports `taxonomy_v2.json` plus `classifier/taxonomy_v2_project_law_topic_seed_assignments.csv`, then exits. It does not import Open Data, fetch documents, redact text, call OpenAI, generate automatic topic assignments, or generate summaries. The matching `ProjectLaw` rows must already exist; seed rows whose `ProjectLawId` is not present are skipped and counted as `missingProjectLaws`.

Redact/process initiative text documents:

```powershell
dotnet run --project backend/src -- parliament-documents redact --legislature XVII --max-documents 25
dotnet run --project backend/src -- parliament-documents redact --legislatures XVII,XVI,XV --max-documents 25
dotnet run --project backend/src -- parliament-documents redact --project-law-id 123
dotnet run --project backend/src -- parliament-documents redact --project-law-id 123 --force-upsert
```

Compare PdfPig and iText extraction for a local PDF without changing stored document content:

```powershell
dotnet run --project backend/src -- parliament-documents compare-pdf-extractors --file C:\path\proposal.pdf
dotnet run --project backend/src -- parliament-documents compare-pdf-extractors --file C:\path\proposal.pdf --output-dir tmp/pdf-extractor-comparison
```

The comparator writes side-by-side `.pdfpig.txt` and `.itext.txt` files so extraction regressions can be checked before changing the production extractor.

The document command only processes imported `InitiativeText` documents whose URL matches `ProjectLaw.FullProposalTextLink`. It does not redact events, votes, interventions, publications, or other `ProjectLaw` metadata.

Before redacting a legislature, the command checks whether `ParliamentDeputies`, `ParliamentaryGroups`, and `ParliamentRedactionTerms` already contain rows for that legislature. If any of them are empty, it imports that legislature's configured base-info JSON first; this prevents weak redaction runs caused by missing deputy or parliamentary group terms.

Repeated redaction commands are idempotent by default. If the source hash, extractor version, renderer version, and redaction policy version are unchanged and the previous run succeeded, the document is skipped. Use `--force-upsert` or `--force` to rewrite the stored redacted model, HTML, and plain text anyway.

If a refresh fails after a document already has successful redacted content, the command preserves that last-good content and stores the latest error message on the row. This includes Parliament unavailable-resource responses such as "O recurso ao qual tentou aceder..." that can be returned from official document URLs while the original proposal document is temporarily unavailable.

Changing the production PDF extractor or extractor version intentionally makes existing PDF document content stale. Re-run the redaction command for the affected initiatives or legislature to regenerate the stored redacted model, HTML, and plain text from the new extractor output.

Do not run this as a second `dotnet run` inside the same live `dev-parlamento-be` container while `dotnet watch` is running. The dev container watches mounted project directories, and a second `dotnet run` can mutate `obj/` while the watcher scans it. Prefer a one-shot container command or run the compiled application directly in a shell where `dotnet watch` is not active.

### AI summary pipeline

The AI summary pipeline is independent from both Open Data import and document redaction. It never reads PDFs and never summarizes HTML. It only summarizes `ParliamentDocumentContents.RedactedContentText` after the document redaction pipeline has succeeded.

Configure OpenAI with environment variables:

```text
OPENAI_API_KEY=
OPENAI_MODEL=gpt-4o-mini-2024-07-18
OPENAI_LONG_CONTEXT_FALLBACK_MODEL=gpt-4.1
```

`OPENAI_API_KEY` is required for real summary generation and must not be committed with a value. `OPENAI_MODEL` defaults to `gpt-4o-mini-2024-07-18` and can be changed later without code changes. If the primary model rejects a document because the prompt exceeds its context window, the summary client retries once with `OPENAI_LONG_CONTEXT_FALLBACK_MODEL`, which defaults to `gpt-4.1`. The configured temperature is `0.1`, chosen for low-variance factual summaries while still allowing natural phrasing.

Run summaries manually:

```powershell
dotnet run --project backend/src -- parliament-summary --initiative-id 123
dotnet run --project backend/src -- parliament-summary --all-unprocessed
dotnet run --project backend/src -- parliament-summary --legislature XVII
dotnet run --project backend/src -- parliament-summary --force
dotnet run --project backend/src -- parliament-summary --legislature XVII --max-documents 25
```

The summary command is idempotent. It skips already succeeded summaries when the source document hash, model name, and prompt version match. Use `--force` to regenerate matching summaries. Failed generations are logged and stored with status/error details, but they do not stop the batch. Empty and extremely short redacted texts are skipped. If a forced refresh fails or becomes skipped for a document that already has a successful summary for the same source hash/model/prompt version, the successful summary remains visible and the latest refresh error is stored without downgrading its status.

Summaries are stored in `ParliamentSummaries` with:

- short title, paragraph summary, and bullet points JSON
- generation timestamp
- model name
- prompt version
- source document hash
- generation status
- error message/details

Changing the prompt version, model, or redacted source hash makes old summaries identifiable as stale so they can be regenerated selectively.

The active prompt requires neutral European Portuguese (`pt-PT`) output and forbids political opinions, recommendations, advocacy, speculation, emotional language, and mixed Portuguese variants.

### Proposal topic taxonomy pipeline

The proposal topic pipeline stores the reviewed taxonomy and assignment metadata in normalized tables:

- `ProposalTopicTaxonomyVersions`
- `ProposalTopicParents`
- `ProposalSubtopics`
- `ProjectLawTopicAssignments`

Production artifacts are bundled under:

```text
Infrastructure/Data/ProposalTopics/taxonomy_v2/
```

The seed pipeline imports parent topics and subtopics from `taxonomy_v2.json`, imports historical reviewed assignments from `classifier/taxonomy_v2_project_law_topic_seed_assignments.csv`, and uses `classifier/taxonomy_v2_centroid_classifier.json` for automatic assignment of new proposals. Historical rows are matched by existing `ProjectLawId`; rows for proposals not present in the app database are skipped and counted in command output.

To load only the reviewed taxonomy and historical reviewed assignments after proposals already exist, run:

```powershell
dotnet run --project backend/src -- parliament-topics seed-reviewed
```

Automatic assignment only reads `ParliamentDocumentContents.RedactedContentText`. It does not use proposer identity, party identity, parliamentary group identity, party vote information, user votes, or user behavior. The classifier embeds normalized redacted text with `text-embedding-3-large`, splits it into 750-word chunks with 75-word overlap, mean-pools chunk embeddings, L2-normalizes the proposal vector, and compares it to normalized subtopic centroids by dot product.

Configure topic assignment with environment variables:

```text
OPENAI_API_KEY=
OPENAI_EMBEDDING_MODEL=text-embedding-3-large
ProposalTopics__TaxonomyVersion=taxonomy_v2
ProposalTopics__ArtifactDirectory=Data/ProposalTopics/taxonomy_v2
ProposalTopics__MinimumRedactedWordCount=20
ProposalTopics__ChunkWordCount=750
ProposalTopics__ChunkOverlapWordCount=75
```

`OPENAI_API_KEY` is shared with summaries by default, but the embedding model is separate from `OPENAI_MODEL`. Use `ProposalTopics__ApiKey` if topic assignment should use a different secret source.

The classifier stores:

- accepted automatic assignments as `centroid_auto_assigned` / `assigned`
- near-threshold candidates as `centroid_needs_review` / `needs_review`
- low-similarity or unusable text as `unassigned` / `unassigned`

Reviewed or seeded assignments are not overwritten by automatic assignment. Parent topic text is derived through `ProjectLawTopicAssignment -> ProposalSubtopic -> ProposalTopicParent` rather than repeated on every proposal.

### HTTP import endpoints

Manual import endpoints are exposed by `ParliamentImportController`:

```text
POST /parliament-import/local-file
POST /parliament-import/legislatures/{legislature}
POST /parliament-import/legislatures
POST /parliament-import/base-info/{legislature}
POST /parliament-import/base-info/{legislature}/local-file
POST /parliament-import/documents/redact
```

Example local-file body:

```json
{
  "filePath": "docs/samples/example_iniciativa.json"
}
```

Example multi-legislature body:

```json
{
  "legislatures": ["XVII", "XVI", "XV"]
}
```

Example base-info local-file body:

```json
{
  "filePath": "docs/samples/base_info_xvii.json"
}
```

Example document redaction body:

```json
{
  "legislature": "XVII",
  "projectLawId": null,
  "maxDocuments": 10
}
```

### Daily background job

The backend registers a hosted service for the scheduled latest-legislature seed pipeline.

When enabled, it runs once per day at `ParliamentOpenData:DailyImport:RunAt` in `ParliamentOpenData:DailyImport:TimeZoneId`, and processes only `ParliamentOpenData:LatestLegislature`. Older legislatures should be seeded manually with `parliament-seed`.

The daily job runs import, base-info preflight, document extraction/redaction, and summaries when `ParliamentOpenData:DailyImport:RunSummaries=true`. If summaries are enabled but `OPENAI_API_KEY` is not configured, the job logs a warning and skips the summary phase while still importing and redacting.

Enable it with environment variables:

```powershell
$env:ParliamentOpenData__DailyImport__Enabled="true"
$env:ParliamentOpenData__DailyImport__RunSummaries="true"
$env:ParliamentOpenData__LatestLegislature="XVII"
```

The daily job is intended for the latest legislature only. It relies on the same idempotency checks as `parliament-seed`, so proposals already inserted are ignored unless their source hash or downstream extractor/redaction/summary version inputs change.

### Remaining migration-spec work

Important remaining work from `docs/SCRAPER_MIGRATION_SPEC.md`:

- add reliable PDF extraction with formatting preservation where possible
- expose redacted document content through frontend-facing API DTOs/views
- decide whether to replace legacy `ProjectLaw.ProposalTextHTML` with `ParliamentDocumentContents.RedactedContentHtml`
- add OCR support for scanned-image PDFs
- add the AI summary pipeline later, generated from redacted text only
- add summary audit tables/jobs with prompt version, model, input hash, status, and retries
- add summary invalidation when source document hash or redaction policy changes
- tune redaction policy with real false-positive/false-negative examples

## Local run without Docker

From `backend/src/`:

```powershell
dotnet run
```

For local PostgreSQL, set `ConnectionStrings__DefaultConnection` first.

## Tests

Unit tests:

```powershell
dotnet test backend/test/test.csproj
```

Integration tests:

```powershell
dotnet test backend/integration-tests/integration-tests.csproj
```

## API documentation

Swagger/OpenAPI is exposed by the development API at:

`http://localhost:8180/swagger/index.html`

Health check endpoint:

`http://localhost:8180/healthz`
