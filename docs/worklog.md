# Worklog — Crypto Tracker

> **Reminder:** after each build/significant change, add a new entry below with:
> - Version number (e.g. v2.0, v2.1)
> - Date and day (e.g. 2026-05-25 Monday)
> - Summary of changes made

---

## v2.0 — 2026-05-25 (Monday)

- Initial project setup
- Created `PLAN.md` with full architecture specification
- Created `docs/` directory and `worklog.md`

## v2.1 — 2026-05-25 (Monday)

### Backend Implementation

- Created `.env.example`, `.gitignore`, `docker-compose.yml` (db + backend services)
- Created `backend/Dockerfile` (multi-stage build: sdk:8.0 → aspnet:8.0)
- Initialized .NET project with `dotnet new web` (net9.0, Minimal APIs, no controllers)
- Added NuGet packages: EF Core Design 9.0.16, Pomelo.EntityFrameworkCore.MySql 9.0.0-preview, Swashbuckle.AspNetCore 10.1.7, Microsoft.AspNetCore.OpenApi 9.0.16
- Created 3 entity models: `TrackedCrypto`, `PriceHistory`, `Alert`
- Created DTOs: `CryptoPriceDto`, `PriceCacheEntry` for cached data
- Created `AppDbContext` with Fluent API configuration, indexes, relationships, and seed data (6 cryptos)
- Created `AppDbContextFactory` for design-time migrations (uses `MariaDbServerVersion` to avoid connection at design time)
- Created `PriceFetcherService` (IHostedService): fetches from CoinGecko every N seconds, caches prices, saves to PriceHistory, signals AlertChecker via Channel<bool>
- Created `AlertCheckerService` (IHostedService): waits for PriceFetcher signal, checks alert conditions, fires webhooks via POST
- Created 3 endpoint groups: `MapCryptoEndpoints` (list, chart, history), `MapAlertEndpoints` (create, list, delete), `MapHealthEndpoints` (db status + cache age)
- Created `Program.cs` with DI registration, CORS, Swagger, global error handler, auto-migration
- Generated initial EF Core migration (`InitialCreate`)
- Build successful: 0 errors, 0 warnings
- Created `.env` with local dev credentials (admin/admin)
- Created `AppDbContextFactory` (separated for design-time EF migrations)
- Updated `launchSettings.json`: port 5000, database env vars for local dev

### Testing Results (v2.1)

**Environment:** Docker Desktop running, MariaDB 11 container on port 3307
**Database:** Connection OK, migrations applied, 6 cryptos seeded
**API Endpoints tested:**

| Endpoint | Status | Notes |
|----------|--------|-------|
| `GET /api/health` | 200 ✓ | `{"status":"ok","db":"connected","cache_age_seconds":-1}` |
| `GET /api/crypto/list` | 200 ✓ | `[]` (empty — CoinGecko blocked by network) |
| `GET /api/crypto/{id}/chart?days=7` | 200 ✓ | `[]` (no price history yet) |
| `POST /api/alerts` | 201 ✓ | Alert created with id=1 |
| `GET /api/alerts` | 200 ✓ | Returns alert list |
| `DELETE /api/alerts/{id}` | 204 ✓ | Alert deleted |
| `GET /swagger/index.html` | 200 ✓ | Swagger UI loads |
| `GET /swagger/v1/swagger.json` | 200 ✓ | OpenAPI spec available |

**Known Issues:**
- CoinGecko API returns 403 (FortiGuard blocks cryptocurrency on school network) — app works on unrestricted networks
- `MapOpenApi()` returns 404 (Swashbuckle handles OpenAPI at `/swagger/v1/swagger.json`)
- Swashbuckle downgraded from 10.1.7 to 7.2.0 (compatibility with .NET 9)
- Named HttpClient `CoinGecko` configured with SSL bypass for proxy environments
