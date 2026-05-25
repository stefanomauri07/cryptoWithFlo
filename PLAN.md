# Crypto Tracker — Implementation Plan (v2)

## Architecture Overview

```
┌──────────────────────────────────────────────────────────┐
│                    Docker Compose                         │
│                                                          │
│  frontend (nginx:alpine, :3000)                          │
│     │                                                    │
│     ▼                                                    │
│  backend (ASP.NET Core 8, :5000)                         │
│     │           │              │                         │
│     ▼           ▼              ▼                         │
│  MariaDB     CoinGecko      Webhook URLs                 │
│  (:3307)     (public API)   (user-configured)            │
└──────────────────────────────────────────────────────────┘
```

---

## 1. Project Structure

```
crypto-tracker/
├── PLAN.md
├── docs/
│   └── worklog.md              # daily changelog per build
├── .env                         # gitignored
├── .env.example                 # committed
├── .gitignore
├── docker-compose.yml
├── README.md
├── backend/
│   ├── CryptoApp.csproj
│   ├── Dockerfile
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Endpoints/
│   │   ├── CryptoEndpoints.cs
│   │   ├── AlertEndpoints.cs
│   │   └── HealthEndpoints.cs
│   ├── Models/
│   │   ├── TrackedCrypto.cs
│   │   ├── PriceHistory.cs
│   │   └── Alert.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Services/
│   │   ├── PriceFetcherService.cs
│   │   └── AlertCheckerService.cs
│   └── Migrations/              # EF-generated
└── frontend/
    ├── Dockerfile
    ├── default.conf             # nginx config
    ├── index.html
    ├── main.js
    ├── style.css
    └── config.js
```

Endpoints are organized in separate files via `RouteGroupBuilder` extension methods to keep `Program.cs` clean (~60 lines max).

---

## 2. Backend — ASP.NET Core (.NET 8, Minimal APIs) + Swagger

### 2a. `Program.cs`

```csharp
// 1. Build app
// 2. Register DI: IMemoryCache, IHttpClientFactory, DbContext (Pomelo),
//    both IHostedService workers, Channel<bool> singleton
// 3. Configure CORS (localhost:3000)
// 4. AddEndpointsApiExplorer + AddSwaggerGen
// 5. MapOpenApi + UseSwaggerUI (dev only)
// 6. app.MapCryptoEndpoints()
//    app.MapAlertEndpoints()
//    app.MapHealthEndpoints()
// 7. Use global ExceptionHandler middleware
// 8. EnsureCreatedOrMigrate on startup
// 9. Run
```

### 2b. Endpoints

#### `Endpoints/CryptoEndpoints.cs`
```
GET  /api/crypto/list
     → Returns current prices for tracked cryptos from IMemoryCache["prices"]:
       [{ id, name, symbol, price_usd, price_eur, change_24h_percent }]

GET  /api/crypto/{id}/chart?days=7
     → Returns chart data from PriceHistory table:
       [{ timestamp, price_usd }]
       Default days = 7.

GET  /api/crypto/{id}/history?from=YYYY-MM-DD&to=YYYY-MM-DD
     → Returns full price history for a custom date range.
```

#### `Endpoints/AlertEndpoints.cs`
```
GET    /api/alerts
       → Returns list of all configured price alerts.

POST   /api/alerts
       → Creates a new price alert:
         Body: { cryptoId, condition: "above"|"below", thresholdUsd, webhookUrl }
         Validates condition enum. Returns 201.

DELETE /api/alerts/{id}
       → Deletes an alert by id. Returns 204.
```

#### `Endpoints/HealthEndpoints.cs`
```
GET  /api/health
     → Returns { status: "ok", db: "connected"|"error", cache_age_seconds: N }
```

### 2c. Background Services — Coordinated Execution

Uses `System.Threading.Channels` to coordinate the two services:
- Registered as singleton: `Channel.CreateBounded<bool>(1)`
- PriceFetcher writes to the channel when done, AlertChecker reads from it before checking.

#### `PriceFetcherService` (IHostedService)
```
Loop (every FETCH_INTERVAL_SECONDS):
  1. Call CoinGecko: /api/v3/simple/price?ids=bitcoin,ethereum,...&vs_currencies=usd,eur&include_24hr_change=true
     → Single call fetches all 6 cryptos (rate-limit friendly)
     → Uses X-Cg-Pro-Api-Key header if COINGECKO_API_KEY is set
  2. Update IMemoryCache["prices"] with full response (TTL = 3 min)
  3. Bulk-insert rows into PriceHistory (DbSet.AddRange)
  4. Write to Channel<bool> to signal completion
  5. await Task.Delay(FETCH_INTERVAL_SECONDS * 1000)
```

#### `AlertCheckerService` (IHostedService)
```
Loop:
  1. await channel.Reader.ReadAsync()   // waits for PriceFetcher to finish
  2. Read all alerts where IsTriggered == false
  3. Compare cached price vs threshold
  4. For each triggered alert:
     a. POST to webhook_url with JSON payload:
        { crypto_id, price_usd, threshold_usd, condition, triggered_at }
     b. Set IsTriggered = true, TriggeredAt = DateTime.UtcNow
  5. await Task.Delay(ALERT_CHECK_INTERVAL_SECONDS * 1000)
```

### 2d. EF Core Models

```csharp
// Models/TrackedCrypto.cs
public class TrackedCrypto
{
    [Key]
    public string Id { get; set; }          // "bitcoin", "ethereum", etc.
    public string Name { get; set; }         // "Bitcoin"
    public string Symbol { get; set; }       // "BTC"
    public bool IsActive { get; set; }

    public ICollection<PriceHistory> PriceHistories { get; set; }
    public ICollection<Alert> Alerts { get; set; }
}

// Models/PriceHistory.cs
public class PriceHistory
{
    public int Id { get; set; }
    public string CryptoId { get; set; }
    public decimal PriceUsd { get; set; }
    public decimal PriceEur { get; set; }
    public decimal Change24hPercent { get; set; }
    public DateTime RecordedAt { get; set; }  // UTC

    public TrackedCrypto Crypto { get; set; }
}

// Models/Alert.cs
public class Alert
{
    public int Id { get; set; }
    public string CryptoId { get; set; }
    public string Condition { get; set; }     // "above" or "below"
    public decimal ThresholdUsd { get; set; }
    public string WebhookUrl { get; set; }
    public bool IsTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TriggeredAt { get; set; }

    public TrackedCrypto Crypto { get; set; }
}
```

### 2e. Seed Data

```csharp
modelBuilder.Entity<TrackedCrypto>().HasData(
    new { Id = "bitcoin",  Name = "Bitcoin",  Symbol = "BTC", IsActive = true },
    new { Id = "ethereum", Name = "Ethereum", Symbol = "ETH", IsActive = true },
    new { Id = "cardano",  Name = "Cardano",  Symbol = "ADA", IsActive = true },
    new { Id = "dogecoin", Name = "Dogecoin", Symbol = "DOGE", IsActive = true },
    new { Id = "solana",   Name = "Solana",   Symbol = "SOL", IsActive = true },
    new { Id = "ripple",   Name = "Ripple",   Symbol = "XRP", IsActive = true }
);
```

### 2f. Configuration (Environment Variables)

```
DATABASE_HOST          // default: "localhost"
DATABASE_PORT          // default: "3307"
DATABASE_NAME          // default: "crypto_tracker"
DATABASE_USER          // default: "app_user"
DATABASE_PASSWORD      // required
COINGECKO_API_KEY      // optional — adds X-Cg-Pro-Api-Key header
FETCH_INTERVAL_SECONDS       // default: 120
ALERT_CHECK_INTERVAL_SECONDS // default: 120
```

Connection string built programmatically:
```csharp
var connStr = $"Server={host};Port={port};Database={db};User={user};Password={pw};";
```

### 2g. Global Error Handler

```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(feature.Error, "Unhandled exception");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
    });
});
```

### 2h. Swagger / OpenAPI

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// After app.Build():
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapOpenApi();  // available at /openapi/v1.json
```

---

## 3. Database — MariaDB (docker-compose.yml)

```yaml
services:
  db:
    image: mariadb:11
    container_name: crypto-db
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${DATABASE_NAME}
      MYSQL_USER: ${DATABASE_USER}
      MYSQL_PASSWORD: ${DATABASE_PASSWORD}
    ports:
      - "3307:3306"
    volumes:
      - mariadb_data:/var/lib/mysql
    healthcheck:
      test: ["CMD", "healthcheck.sh", "--connect", "--innodb_initialized"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s

  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: crypto-backend
    ports:
      - "5000:5000"
    env_file:
      - .env
    depends_on:
      db:
        condition: service_healthy

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: crypto-frontend
    ports:
      - "3000:80"
    depends_on:
      - backend

volumes:
  mariadb_data:
```

---

## 4. Environment Files

### `.env.example`
```env
DATABASE_HOST=db
DATABASE_PORT=3306
DATABASE_NAME=crypto_tracker
DATABASE_USER=app_user
DATABASE_PASSWORD=your_password_here
MYSQL_ROOT_PASSWORD=your_root_password_here
COINGECKO_API_KEY=
FETCH_INTERVAL_SECONDS=120
ALERT_CHECK_INTERVAL_SECONDS=120
API_URL=http://localhost:5000
```

### `.env` (gitignored)
Same keys, real passwords.

### `.gitignore`
```
.env
backend/bin/
backend/obj/
frontend/node_modules/
.vs/
*.user
```

---

## 5. Frontend

### 5a. Files

| File | Purpose |
|------|---------|
| `config.js` | `const API_URL = window.__API_URL__ || "http://localhost:5000";` |
| `Dockerfile` | `nginx:alpine`, copies `*.html *.js *.css default.conf` |
| `default.conf` | Nginx config serving static files |

### 5b. UI Sections

| Section | Data Source | Refresh | Notes |
|---------|-----------|---------|-------|
| **Health Bar** | `GET /api/health` | 60s | DB status dot (green/red) + cache age |
| **Price Table** | `GET /api/crypto/list` | 30s | Green ▲ + red ▼, CSS transition on value change |
| **Chart** | `GET /api/crypto/{id}/chart?days=N` | on selection | Chart.js line, gradient fill, `tension: 0.4` |
| **Alerts Panel** | `GET /api/alerts`, `POST/DELETE` | on action | Form + list, Active/Triggered badges |

### 5c. Dockerfiles

#### `frontend/Dockerfile`
```dockerfile
FROM nginx:alpine
COPY default.conf /etc/nginx/conf.d/default.conf
COPY *.html *.js *.css /usr/share/nginx/html/
EXPOSE 80
```

#### `nginx default.conf`
```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location ~* \.js$ {
        default_type application/javascript;
    }
}
```

#### `backend/Dockerfile` (multi-stage)
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "CryptoApp.dll"]
```

---

## 6. API Reference

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/api/crypto/list` | `[{id, name, symbol, price_usd, price_eur, change_24h_percent}]` |
| `GET` | `/api/crypto/{id}/chart?days=7` | `[{timestamp, price_usd}]` |
| `GET` | `/api/crypto/{id}/history?from=...&to=...` | `[{timestamp, price_usd, price_eur, change_24h_percent}]` |
| `GET` | `/api/alerts` | `[{id, cryptoId, condition, thresholdUsd, webhookUrl, isTriggered, ...}]` |
| `POST` | `/api/alerts` | Body: `{cryptoId, condition, thresholdUsd, webhookUrl}` → 201 |
| `DELETE` | `/api/alerts/{id}` | 204 |
| `GET` | `/api/health` | `{status, db, cache_age_seconds}` |
| `GET` | `/swagger` | Swagger UI (dev only) |
| `GET` | `/openapi/v1.json` | OpenAPI spec |

---

## 7. Delivery Checklist

- [ ] 1. `.env.example` + `.gitignore`
- [ ] 2. `docker-compose.yml`
- [ ] 3. `backend/Dockerfile`
- [ ] 4. `backend/CryptoApp.csproj` (NuGet packages)
- [ ] 5. `backend/Models/` (3 entity classes)
- [ ] 6. `backend/Data/AppDbContext.cs`
- [ ] 7. `backend/Services/PriceFetcherService.cs`
- [ ] 8. `backend/Services/AlertCheckerService.cs`
- [ ] 9. `backend/Endpoints/` (Crypto, Alert, Health)
- [ ] 10. `backend/Program.cs` (DI, middleware, mapping)
- [ ] 11. `frontend/Dockerfile` + `default.conf`
- [ ] 12. `frontend/index.html` + `style.css` + `main.js` + `config.js`
- [ ] 13. `README.md`
