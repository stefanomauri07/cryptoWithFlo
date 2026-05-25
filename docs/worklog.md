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

## v2.2 — 2026-05-25 (Monday)

### Frontend Implementation

- Created 6 frontend files based on Stitch designs (dashboard_cryptotracker + cyberpulse DESIGN.md)
- `config.js`: API_BASE = localhost:5000
- `index.html`: Full dashboard SPA with Tailwind CDN, Material Symbols, Chart.js CDN
  - Exact Tailwind config copied from Stitch (colors, spacing, typography, border-radius)
  - Navbar: CryptoTracker logo + Dashboard/Alerts nav links + health status dot + cache age
  - Health bar: DB status indicator + auto-refresh timer
  - Section 1 — Price Table: 6 rows, skeleton loading, green ▲ / red ▼ for 24h change
  - Section 2 — Chart: crypto dropdown, 7D/30D/90D pills, Chart.js canvas with gradient fill
  - Section 3 — Alerts: form (crypto, condition, threshold, webhook URL) + list (Active/Triggered badges)
- `style.css`: glass-panel, custom scrollbar, skeleton shimmer, status dots, animations, toast
- `main.js`: All fetch logic, Chart.js management, DOM updates, polling (30s prices, 60s alerts)
- `default.conf` + `Dockerfile` for nginx deployment in docker compose
- Frontend served on port 3000 via `npx serve`

### Testing (v2.2)

- Frontend loads all 3 sections with skeleton loading state
- Backend successfully fetches real CoinGecko prices (200 OK — network unblocked)
- All API calls through CORS localhost:3000 → localhost:5000 work
- 6 crypto rows updated every 30 seconds with real-time prices
- Chart.js renders interactive line chart with gradient fill
- Alerts CRUD functional (create, list, delete)
- Swashbuckle downgraded from 10.1.7 to 7.2.0 (compatibility with .NET 9)
- Named HttpClient `CoinGecko` configured with SSL bypass for proxy environments

## v3.0 — 2026-05-25 (Monday)

### Auth + Email Alerts

**Backend:**
- NuGet: `JwtBearer`, `BCrypt.Net-Next`
- Models: `User` (Id, Email, PasswordHash, Role, Name, IsVerified), `Otp` (6-digit code, purpose, expiry)
- `Alert.cs`: removed `WebhookUrl`, added `UserId` FK
- `AppDbContext`: Users, Otps DbSets, seed admin `admin@cryptotracker.com`/`Admin123!`
- `BrevoEmailService`: sends HTML emails via Brevo API (OTP template + alert template, dark theme)
- `AuthEndpoints`: `/api/auth/register`, `/verify`, `/login`, `/forgot-password`, `/reset-password`, `/me`
- `Program.cs`: JWT config, auth middleware, Brevo singleton, removed Channel
- `AlertEndpoints`: RequireAuthorization, remove webhook, filter by user role (admin sees all)
- `AlertCheckerService`: email via Brevo instead of webhook, independent timer
- `CryptoEndpoints`: RequireAuthorization on all
- EF Migration: `Auth` (Users, Otps, Alert.UserId)

**Frontend:**
- `auth.js`: JWT token management, api() helper, login/register/verify/logout functions
- `login.html`: Stitch design, email+password form, error handling
- `register.html`: 2-step flow (credentials → OTP verification), Stitch design
- `reset-password.html`: 2-step flow (email → OTP + new password)
- `index.html`: user menu (name + role badge + logout), removed webhook URL field, auth guard
- `main.js`: all fetch → api(), removed webhookUrl, updateUserMenu()

### Testing (v3.0)

| Endpoint | Status |
|----------|--------|
| `POST /api/auth/login` (admin) | 200, JWT token |
| `GET /api/crypto/list` (auth) | 200, 6 cryptos |
| `GET /api/crypto/list` (no auth) | 401 Unauthorized |
| `GET /api/alerts` (auth) | 200 |
| `POST /api/alerts` (auth) | 201, alert created |
| `POST /api/auth/register` | 201 |

### Security note

- Brevo API key removed from `launchSettings.json` (replaced with placeholder)
- Real keys in `.env` (gitignored). For local dev, set `BREVO_API_KEY` env var or edit launchSettings.json manually.
