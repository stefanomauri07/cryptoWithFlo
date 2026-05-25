# CryptoTracker Worklog

## v3.1 (2026-05-25) — Multi-page SPA + Stitch Design Integration

### Fixed
- Dockerfile backend: .NET 8 → .NET 9 SDK/runtime (build ora corretto)
- docker-compose.yml: aggiunto servizio frontend (nginx su porta 3000:80)
- config.js: aggiunto fallback `window.__API_URL__`
- index.html: rimosso input threshold duplicato

### Added
- **`layout.js`**: shell condiviso (sidebar fissa, navbar top, footer) iniettato via JS su tutte le pagine autenticate
- **`crypto-detail.html`**: pagina dettaglio crypto con hero header, chart (24H/7D/30D/90D/1Y), stats grid (market cap, volume, ATH, supply), activity feed, sentiment gauge
- **`alert-history.html`**: pagina storico alert con stats bento grid, tabella filtrabile (All/Active/Triggered), search, debug console
- **`market-explorer.html`**: market explorer con marquee ticker animata, category filter pills, tabella con rank/price/change/volume/market cap, click su riga → crypto detail
- **`portfolio.html`**: portfolio con balance hero, asset allocation (donut chart), top gainers, recent transactions, tabella holdings. Dati placeholder (servira' backend holdings in futuro)

### Backend additions
- `CryptoPriceDto`: aggiunti campi `MarketCap`, `Volume24h`, `AllTimeHigh` (nullable)
- `PriceFetcherService`: fetch aggiuntivi `include_market_cap=true&include_24hr_vol=true`
- `GET /api/crypto/{id}/stats`: nuovo endpoint con tutti i campi della crypto dalla cache
- `GET /api/alerts`: aggiunti query params `?status=active|triggered` e `?q=<search>`

### Navigation
- Sidebar: Overview → Dashboard, Assets → Market Explorer, History → Alert History, Portfolio → Portfolio
- Navbar: Dashboard, Markets, Portfolio, Alerts
- Click riga tabella prezzi → crypto-detail.html?id=...
- Click riga market explorer → crypto-detail.html?id=...

### Style
- Aggiunte classi CSS: `.sidebar`, `.sidebar-link`, marquee animation, `.glass-card-hover`, `.table-row-click`, `.donut-chart`, `.tab-pill`, `.debug-console`, `.gauge-bg`

## v3.0 (2026-05-25) — Auth + Email Alerts
- JWT authentication (login, register, OTP verify, password reset)
- Brevo email integration for OTP and alert notifications
- Users/Otps models, Auth endpoints
- Auth guard on frontend, login/register/reset-password pages

## v2.2 (2026-05-25) — Frontend Implementation
- 6 static HTML/JS/CSS files based on Stitch designs
- Chart.js integration, auto-refresh every 30s
- Price table, chart, alerts panel, skeletons, toasts

## v2.1 (2026-05-25) — Backend Implementation
- ASP.NET Core Minimal APIs with EF Core + MariaDB
- Models: TrackedCrypto, PriceHistory, Alert
- Background services: PriceFetcherService, AlertCheckerService
- CoinGecko API integration

## v2.0 (2026-05-25) — Initial Setup
- Docker Compose with MariaDB + .NET backend
- PLAN.md + worklog created
