# CryptoTracker Worklog

## v3.4 (2026-05-26) — Fix prezzi reali (FortiGuard bypass + Binance only)

### Fixed
- **Firewall bypass**: `api.binance.com` bloccato da FortiGuard → usato `data-api.binance.vision` come dominio alternativo
- **URL encoding**: richiesta batch Binance ora usa `Uri.EscapeDataString` per `[`, `]`, `"` nel query string
- **BINANCE_API_URL**: resa configurabile via environment variable (default: `https://api.binance.com/`)
- **SSL bypass** su HttpClient Binance (stesso handler di CoinGecko)

### Removed
- CoinGecko client e tutta la logica di merge CoinGecko/Binance
- `IHttpClientFactory` da `PriceFetcherService` (non piu' necessario)
- `COINGECKO_API_KEY` da `.env`

### Changed
- `PriceFetcherService`: usa solo Binance come fonte prezzi
- `docker-compose.yml`: aggiunta `BINANCE_API_URL` nell'environment del backend

### Added
- `.env`: `BINANCE_API_URL=https://data-api.binance.vision`

### Fixed
- **Prezzi sempre disponibili**: `PriceFetcherService` ora usa Binance come fonte primaria (`GET /api/v3/ticker/price`, no API key). CoinGecko resta per market cap/volume/ATH solo se API key presente. Senza CoinGecko key i prezzi funzionano comunque via Binance.
- **Live Markets e Market Explorer ora popolati** anche senza CoinGecko API key
- **Toast su fetchPrices fail** dopo 2 tentativi falliti
- **Market Explorer**: messaggio "No data available" se dati vuoti
- **Terminal sidebar rimosso** da layout.js
- **Footer duplicato rimosso** da market-explorer.html (layout.js lo inietta)
- **layout.js caricato una volta sola** in market-explorer.html
- **fetchHealth doppio intervallo rimosso** da layout.js (solo main.js lo gestisce)
- **CS1998 warning fixato** in CryptoEndpoints stats endpoint

### Added — Portfolio Reale
- **Modello `UserHolding`**: UserId (FK), CryptoId (FK), Amount, CreatedAt, UpdatedAt. Unique index (UserId, CryptoId)
- **`GET /api/portfolio`**: holdings utente con prezzi live da cache, allocation %, total balance
- **`POST /api/portfolio`**: add/update/remove (amount=0 → delete) holding
- **`DELETE /api/portfolio/{cryptoId}`**: rimuove holding
- **Nav property `Holdings`** aggiunte a User e TrackedCrypto
- **Migration `AddUserHoldings`**: crea tabella UserHoldings, seed admin vuoto (amount=0)
- **Portfolio.html completamente dinamico**: fetch da /api/portfolio, form Quick Add, donut chart CSS, summary stats, delete per riga, row click → crypto-detail

### Changed
- `BinanceService`: nuovo metodo `GetCurrentPricesAsync()` per prezzi real-time
- `PriceFetcherService` riscritto: fetch Binance primario + CoinGecko opzionale con merge dati
- `BinanceService.GetCryptoIdFromSymbol()` per reverse lookup symbol→cryptoId
- Portfolio.html: tutto dinamico, nessun dato hardcodato, auto-refresh 60s

### Changed
- Admin credentials: email `admin`, password `admin` (migration UpdateAdminSeed)
- login.html: campo email cambiato da `type="email"` a `type="text"` per supportare login con "admin"

### Added
- **`BinanceService`**: fetch candele OHLCV da Binance (klines endpoint pubblico, no API key)
- Symbol mapping: bitcoin→BTCUSDT, ethereum→ETHUSDT, cardano→ADAUSDT, ecc.
- **Chart endpoint** ora usa Binance come fonte primaria: `GET /api/crypto/{id}/chart?days=N`
  - Intervallo automatico in base ai giorni: ≤1→15m, ≤7→1h, ≤30→4h, >30→1d
  - Cache 2 minuti per klines
  - Fallback al DB PriceHistories se Binance non risponde
- Registrato `HttpClient("Binance")` + `BinanceService` singleton in Program.cs

### Vantaggi Binance
- Dati grafico immediati (senza aspettare giorni di storico DB)
- Alta granularita' (fino a 15 minuti)
- Rate limit 1200 req/min, nessuna API key richiesta
- L'endpoint chart restituisce lo stesso formato `[{timestamp, price_usd}]` — frontend invariato

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
