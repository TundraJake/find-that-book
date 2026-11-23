# CLAUDE.md — Find That Book

Find That Book: messy text query → Gemini field extraction → Open Library candidates → deterministic ranking → grounded explanations → React UI.

## Working style (one-on-one sessions)
- Fast iteration: short, to-the-point bullets. No long prose, no recaps.
- Small incremental steps; each step leaves the code organized, formatted, and building.
- Surface decisions as quick either/or choices with a recommendation; decide and move on.
- Think systems: keep long-term structure in mind while shipping the next small piece.

## Layout
| Path | Role |
|---|---|
| `src/FindThatBook.Domain` | Entities/value objects (`BookCandidate`, `QueryHypothesis`, `MatchTier`). No dependencies. |
| `src/FindThatBook.Application` | Pipeline orchestration (`Services/`), ports (`IQueryExtractor`, `IBookCatalog`, `IExplanationWriter`), `Matching/` (text normalization, title/author matching, tier ranking, de-duplication). Depends on Domain only. |
| `src/FindThatBook.Infrastructure` | Adapters: Gemini client, Open Library HTTP client, caching, resilience. `Gemini/` = query extraction and candidate explanations (options, client, wire models, prompts, schemas, integration status). `OpenLibrary/` = candidate search (options, client with cache + redirect following, wire models, search strategies, work enrichment). |
| `src/FindThatBook.Api` | ASP.NET Core host: `Controllers/`, `Contracts/` (request/response DTOs), DI composition, ProblemDetails. |
| `tests/FindThatBook.UnitTests` | xUnit. Fixtures live in `tests/**/Fixtures/*.json`. |
| `client/` | Vite + React 19 + TypeScript. Dev proxy `/api` → `http://localhost:5080`. |
| `docs/integrations/` | Gemini response reference; Open Library endpoints and data quirks. |
| `docs/DEPLOYMENT.md` | CI/CD, hosting (DigitalOcean App Platform), DNS (Cloudflare), setup checklist. |
| `Dockerfile`, `.do/app.yaml`, `.github/workflows/` | Container image, App Platform spec, CI and Deploy workflows. |

## Architecture: Model–View–Controller
| Role | Lives in | Rules |
|---|---|---|
| **View** | `client/` (React) | Renders state and calls the API through `client/src/api/`. Never calls Gemini or Open Library directly (API keys stay server-side). |
| **Controller** | `src/FindThatBook.Api/Controllers/` | Thin: bind + validate the request contract, call one Application service, map the result to a response contract. No business logic, no HTTP calls to external services. |
| **Model** | `Api/Contracts/` (wire DTOs), `Domain/` (business types), `Infrastructure/**/Models/` (external wire formats) | Three separate model kinds; never expose Domain or external-service types directly as API responses. Map explicitly (`FromDomain`). |
| **Services** | `Application/` (interfaces + orchestration), `Infrastructure/` (implementations) | All logic and external calls live here, behind interfaces registered in DI. |

Dependency direction: `Api → Application → Domain`; `Infrastructure → Application`. Domain depends on nothing.

## No hard-coded values
- **No magic numbers or strings in logic.** Give every literal a name.
- Environment- or deployment-dependent values (URLs, model names, timeouts, limits, CORS origins, API keys) come from configuration: a strongly typed options class (e.g. `GeminiOptions`) bound from `appsettings.json` / user-secrets / environment variables.
- Fixed values that never vary by environment are named constants next to the code that owns them (`const`/`static readonly` in C#, module-level `const` in TypeScript), e.g. `BookSearchRequest.MaximumQueryLength`.
- Shared values used by both client and server (e.g. maximum query length) are defined once on the server and mirrored in one named client constant with a comment pointing at the source.
- Prompt text for LLM calls lives in dedicated prompt files/classes, not inline in service code.
- User-facing UI copy may stay inline in components for now; extract if localization is ever needed.
- Allowed literals: `0`, `1`, `-1`, empty string, and values whose meaning is obvious from an adjacent name (e.g. `TimeSpan.FromSeconds(timeoutSeconds)`).

## Pipeline (request flow)
1. `POST /api/books/search` `{ "query": "..." }`
2. **Extract** — `IQueryExtractor` (Gemini, JSON schema output) → `QueryHypothesis { Title?, Author?, Keywords[], CharacterHints[], YearHint? }`. On failure the request fails with a 502 ProblemDetails.
3. **Retrieve** — `IBookCatalog` runs Open Library `/search.json` strategies in parallel (title+author, title, author, free-text q).
4. **Enrich** — `/works/{id}.json` for canonical `authors`; `/authors/{id}.json` for names. Cached.
5. **Normalize + dedupe** — by work key.
6. **Rank** — deterministic `MatchTier` match hierarchy (tiers a–e). LLM never invents or reorders across tiers.
7. **Explain** — `CandidateExplainer` → `IExplanationWriter` (Gemini, one batched call) writes 1–2 sentences per candidate, grounded only in fetched fields; every candidate must get one or the request fails (502).
8. Return ≤ 5 candidates.

## Conventions
- .NET 10, nullable on, warnings as errors (see `Directory.Build.props`).
- External calls go through typed `HttpClient` + `Microsoft.Extensions.Http.Resilience`.
- Secrets never committed: `dotnet user-secrets` locally, env var `Gemini__ApiKey` in deployment.
- Stateless: no database. `IMemoryCache` only.

## Coding standards
**Adopted**
- Private fields (instance, static, static readonly): `_camelCase`. Constants: `PascalCase`. Enforced at build by `.editorconfig` (IDE1006 as warning → error).
- Full words in identifiers, no acronyms or abbreviations.
- C#: explicit types, named methods, `sealed` by default, records for DTOs.
- Python (any scripts): strict typing, named functions.
- Match the style of the surrounding file.

**Not adopted:** StyleCop.Analyzers, Conventional Commits, Prettier line-width rule.

**API naming:** all API JSON is snake_case (property names, dictionary keys, enum values, ProblemDetails extensions such as `retry_after_seconds` and `trace_id`), following the requested result format (`first_publish_year`). Configured once in `Api/ApiJsonConventions.cs`; the audit log uses the same convention. Outbound wire formats keep their providers' conventions (Gemini camelCase, Open Library snake_case). C# stays PascalCase; the TypeScript types mirror the API's snake_case field names.

## AI usage rules
- Gemini only: no non-AI fallback and no sample/placeholder data anywhere in the request path. Failures surface as errors (502 ProblemDetails).
- Models are tried in `Gemini:Models` order; only HTTP 429 falls back to the next model. HTTP 503 ("high demand") retries the same model (`BusyRetryAttempts` × `BusyRetryDelaySeconds`), never another one, then returns 503 to the client. All models over quota → `AiQuotaExhaustedException` → 429 ProblemDetails with `quotaScope`/`retryAfterSeconds` (friendly notice in the UI, not an error).
- A missing Gemini key does not stop startup: `GET /api/status` reports `ready: false`, the UI disables search and shows the fix.

## AI ranking
- `aiRank` comes from the explanation call (no extra Gemini request). It never changes the API's list order, which follows `MatchTier`; it is an optional client-side sort. Invalid ranks → null, never a failed search.

## Unsafe content
- Safety settings come from configuration (`Gemini:SafetyCategories`, `Gemini:SafetyThreshold`); never hard-code them.
- Blocked prompts/responses and explanations failing `ExplanationContentGuard` → `SearchRejectedException` → 422. No word blocklists.

## Search audit log
- Every `POST /api/books/search` is recorded via `SearchAuditRecorder` → `ISearchAuditLog` (JSON Lines file + app log). Recording must never block or fail the search.
- Unknown values are `ClientLocation.NotAvailable` ("Not available"), never null. Never call external geolocation services; read Cloudflare headers only when `SearchAudit:TrustProxyHeaders` is on.

## Business rules
- At most `BookSearchRules.MaximumCandidates` (5) candidates per search, ordered by `MatchTier`.

## Branches and deployment
- `main` = production (`findthatbook.explorator-maris.com`), `dev` = staging (`findthatbook-dev.explorator-maris.com`).
- Work on feature branches off `dev`; PR into `dev`; promote `dev` → `main` by PR. Never commit directly to `main`.
- Pushes to `dev`/`main` run CI then deploy (deploy is skipped until `DEPLOY_ENABLED=true`). See `docs/DEPLOYMENT.md`.
- Production serves the React build from the API's `wwwroot`; unknown `/api/*` routes return 404, other paths fall back to `index.html`.

## Commands
```bash
dotnet build FindThatBook.slnx
dotnet test FindThatBook.slnx
dotnet run --project src/FindThatBook.Api          # http://localhost:5080
cd client && npm install && npm run dev            # http://localhost:5173
```
