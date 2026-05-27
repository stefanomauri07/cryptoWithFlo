# School Presentation Improvements — Implementation Plan

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Fix critical bugs, add News Feed and Comparative Charts for a polished school presentation.

**Architecture:** .NET 9 Minimal APIs backend with vanilla JS frontend. Docker Compose with MariaDB + Nginx. Binance API for crypto data. Stripe for payments.

**Tech Stack:** C# .NET 9, EF Core, MariaDB, vanilla JS, Chart.js, Tailwind CSS, Docker

---

## Task 1: Fix CoinGecko Toast Message

**Objective:** Replace outdated "Check CoinGecko API key" reference in main.js with "Binance" since CoinGecko is no longer used.

**Files:**
- Modify: `frontend/main.js:119`

**Step 1: Update the error message**

```javascript
// OLD (line 119):
showToast('No price data available. Check CoinGecko API key or network.', true);

// NEW:
showToast('No price data available. Check Binance connection or network.', true);
```

**Step 2: Run the app and verify** — toast message reflects correct API source.

**Step 3: Commit**
```bash
git add frontend/main.js
git commit -m "fix: update toast message from CoinGecko to Binance"
```

---

## Task 2: Document SSL Bypass for School Firewall

**Objective:** Add clear comments explaining why SSL validation is bypassed — the school's FortiGuard firewall blocks Binance's real domain, requiring a workaround.

**Files:**
- Modify: `backend/Program.cs:30-36`
- Modify: `backend/Services/StripeService.cs:17-22`
- Modify: `backend/Services/BrevoEmailService.cs` (if present)

**Step 1: Add explanatory comment above each SSL bypass block**

```csharp
// NOTE FOR SCHOOL PRESENTATION:
// The school firewall (FortiGuard) intercepts HTTPS connections.
// SSL certificate validation is bypassed ONLY for trusted API endpoints
// (Binance, Stripe, Brevo) so the app works on the school network.
// In production, remove this handler and use proper SSL validation.
```

**Step 2: Verify code compiles**

```bash
cd backend && dotnet build 2>&1 | tail -5
```

**Step 3: Commit**
```bash
git add backend/Program.cs backend/Services/StripeService.cs backend/Services/BrevoEmailService.cs
git commit -m "docs: add SSL bypass explanation for school firewall"
```

---

## Task 3: Backend — News Feed Endpoint

**Objective:** Create a news endpoint that fetches crypto news from a free API. Use the free tier of NewsData.io or Cryptopanic (RSS-based, no API key needed).

**Files:**
- Create: `backend/Endpoints/NewsEndpoints.cs`
- Modify: `backend/Program.cs` (register endpoint)

**Architecture:** Use a free RSS feed from Cryptopanic or a simple HTTP fetch to a public news source. For simplicity, we'll use Binance's public announcement endpoint and/or a hardcoded curated feed that can be replaced with a real API.

**Simplest approach (no API key):** Create an endpoint that fetches from multiple free sources:
1. CryptoCompare news (free, no API key for basic tier)
2. Fallback to hardcoded curated news for demo mode

```csharp
// backend/Endpoints/NewsEndpoints.cs
using System.Text.Json;

namespace CryptoApp.Endpoints;

public static class NewsEndpoints
{
    public static RouteGroupBuilder MapNewsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/news");

        group.MapGet("/", async (HttpClient httpClient, IConfiguration config) =>
        {
            try
            {
                // Try CryptoCompare free news API
                var response = await httpClient.GetAsync(
                    "https://min-api.cryptocompare.com/data/v2/news/?lang=EN");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Results.Content(json, "application/json");
                }
            }
            catch { /* fall through to fallback */ }

            // Fallback: return curated news for demo
            return Results.Ok(GetFallbackNews());
        });

        return group;
    }

    private static object[] GetFallbackNews() => new[]
    {
        new { title = "Bitcoin Surges Past $100,000 Milestone", source = "CoinDesk", url = "#", publishedAt = DateTime.UtcNow.AddHours(-2) },
        new { title = "Ethereum 2.0 Finality Upgrade Goes Live", source = "The Block", url = "#", publishedAt = DateTime.UtcNow.AddHours(-4) },
        new { title = "SEC Approves First Spot Bitcoin ETF Options", source = "Reuters", url = "#", publishedAt = DateTime.UtcNow.AddHours(-6) },
        new { title = "Solana DeFi TVL Reaches All-Time High", source = "Decrypt", url = "#", publishedAt = DateTime.UtcNow.AddHours(-8) },
        new { title = "Cardano Partners with African Governments for Digital ID", source = "CoinTelegraph", url = "#", publishedAt = DateTime.UtcNow.AddHours(-12) },
    };
}
```

**Step 1: Create the endpoint file**

Write `backend/Endpoints/NewsEndpoints.cs` with the code above.

**Step 2: Register in Program.cs**

Add after line 126 (MapSubscriptionEndpoints):
```csharp
app.MapNewsEndpoints();
```

**Step 3: Verify build**

```bash
cd backend && dotnet build 2>&1 | tail -5
```

**Step 4: Commit**

```bash
git add backend/Endpoints/NewsEndpoints.cs backend/Program.cs
git commit -m "feat: add news feed endpoint with CryptoCompare + fallback"
```

---

## Task 4: Frontend — News Page

**Objective:** Create a news.html page that shows a feed of crypto news articles.

**Files:**
- Create: `frontend/news.html`

**Design:** Follow existing dark theme. Card-based layout showing title, source, time ago. Clickable cards.

**Step 1: Create news.html**

```html
<!DOCTYPE html>
<html class="dark" lang="en">
<head>
<meta charset="utf-8"/>
<meta content="width=device-width, initial-scale=1.0" name="viewport"/>
<title>News | CryptoTracker</title>
<script src="https://cdn.tailwindcss.com?plugins=forms,container-queries"></script>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;900&display=swap" rel="stylesheet"/>
<link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:wght,FILL@100..700,0..1&display=swap" rel="stylesheet"/>
<link href="style.css" rel="stylesheet"/>
<script id="tailwind-config">/* same config as index.html */</script>
<style>.bg-gradient-main{background:radial-gradient(circle at 0% 0%,#161b2b 0%,#0e1322 100%);}</style>
</head>
<body class="bg-background text-on-surface">
<script src="config.js"></script>
<script src="auth.js"></script>
<script src="layout.js"></script>
<script>
function init() {
    if (!requireAuth()) return;
    updateUserMenu();
    fetchNews();
}

async function fetchNews() {
    const container = document.getElementById('news-container');
    container.innerHTML = '<div class="text-center py-xl text-on-surface-variant">Loading news...</div>';
    
    try {
        const res = await api('/api/news');
        const data = await res.json();
        
        if (!data || data.length === 0) {
            container.innerHTML = '<div class="text-center py-xl text-on-surface-variant">No news available</div>';
            return;
        }
        
        container.innerHTML = data.map((article, i) => `
            <a href="${article.url || '#'}" target="_blank" rel="noopener"
               class="block bg-surface-container-low rounded-xl border border-outline-variant/20 p-lg hover:border-primary-fixed-dim/30 transition-all duration-200"
               style="animation: fadeInUp 0.3s ease-out ${i * 50}ms both">
                <div class="flex items-start gap-md">
                    <span class="material-symbols-outlined text-primary-fixed-dim mt-1">article</span>
                    <div class="flex-1">
                        <h3 class="text-headline-sm font-headline-sm text-on-surface mb-sm">${article.title}</h3>
                        <div class="flex items-center gap-md text-label-md font-label-md text-on-surface-variant">
                            <span class="flex items-center gap-xs">
                                <span class="material-symbols-outlined text-sm">source</span>
                                ${article.source}
                            </span>
                            <span>${timeAgo(article.publishedAt || article.published_on * 1000)}</span>
                        </div>
                    </div>
                </div>
            </a>
        `).join('');
    } catch (err) {
        container.innerHTML = '<div class="text-center py-xl text-error">Failed to load news. Try again later.</div>';
    }
}
</script>

<main class="lg:pl-64 pt-16 min-h-screen bg-gradient-main">
    <div class="p-margin-desktop max-w-3xl mx-auto">
        <div class="flex items-center gap-md mb-lg">
            <span class="material-symbols-outlined text-primary-fixed-dim text-[32px]">newspaper</span>
            <h1 class="font-headline-lg text-headline-lg text-on-surface">Crypto News</h1>
        </div>
        <div id="news-container" class="space-y-md"></div>
    </div>
</main>
</body>
</html>
```

**Step 2: Add to navigation**

Update `frontend/layout.js` sidebar to include News link:
```javascript
// Add to sidebarLinks array:
{ href: 'news.html', icon: 'newspaper', label: 'News' }
```

**Step 3: Commit**
```bash
git add frontend/news.html frontend/layout.js
git commit -m "feat: add news page with CryptoCompare API integration"
```

---

## Task 5: Add Navigation Link for News

**Objective:** Update layout.js to include the News page in the sidebar navigation.

**Files:**
- Modify: `frontend/layout.js`

**Step 1: Read current sidebarLinks in layout.js**

Find the `sidebarLinks` array.

**Step 2: Add News entry**

```javascript
{ href: 'news.html', icon: 'newspaper', label: 'News' },
```

Place between "Portfolio" and the upgrade link.

**Step 3: Commit**
```bash
git add frontend/layout.js
git commit -m "feat: add News link to sidebar navigation"
```

---

## Task 6: Backend — Comparative Chart Endpoint

**Objective:** Create an endpoint that returns chart data for TWO crypto assets simultaneously.

**Files:**
- Modify: `backend/Endpoints/CryptoEndpoints.cs`

**Step 1: Add new endpoint to CryptoEndpoints**

```csharp
// Add to MapCryptoEndpoints:
group.MapGet("/compare/{crypto1}/{crypto2}", async (
    string crypto1, string crypto2, int days,
    AppDbContext db, BinanceService binance, IMemoryCache cache) =>
{
    var data1 = await GetChartData(crypto1, days, db, binance, cache);
    var data2 = await GetChartData(crypto2, days, db, binance, cache);
    
    return Results.Ok(new
    {
        crypto1 = new { id = crypto1, data = data1 },
        crypto2 = new { id = crypto2, data = data2 }
    });
});
```

Extract the existing chart data logic into a private helper `GetChartData()` to avoid duplication.

**Step 2: Verify build**
```bash
cd backend && dotnet build
```

**Step 3: Commit**
```bash
git add backend/Endpoints/CryptoEndpoints.cs
git commit -m "feat: add comparative chart endpoint"
```

---

## Task 7: Frontend — Compare Page

**Objective:** Create compare.html showing two crypto charts overlaid for direct comparison.

**Files:**
- Create: `frontend/compare.html`

**Design:** Two selectors at the top, one overlaid chart with two lines (different colors), percentage change comparison cards.

**Step 1: Create compare.html**

Page with:
- Two crypto selectors
- Overlaid Chart.js chart (two datasets)
- Stats comparison table (price, 24h change, market cap)

**Step 2: Add to navigation in layout.js**
```javascript
{ href: 'compare.html', icon: 'compare_arrows', label: 'Compare' },
```

**Step 3: Commit**
```bash
git add frontend/compare.html frontend/layout.js
git commit -m "feat: add crypto comparison page with overlaid charts"
```

---

## Task 8: Update Worklog

**Objective:** Document all changes in the worklog.

**Files:**
- Modify: `docs/worklog.md`

**Step 1: Add new entry at the top**

```markdown
## v3.5 (2026-05-27) — School Presentation Improvements

### Fixed
- Toast message updated from CoinGecko to Binance reference
- SSL bypass documented with FortiGuard explanation in all services

### Added
- **News page**: `/news.html` — crypto news feed from CryptoCompare API (free tier)
- **Compare page**: `/compare.html` — overlaid charts for comparing two crypto assets
- **News endpoint**: `GET /api/news` — fetches from CryptoCompare, falls back to curated demo data
- **Compare endpoint**: `GET /api/crypto/compare/{crypto1}/{crypto2}?days=N` — dual chart data

### Changed
- `layout.js`: added News and Compare sidebar links
```

**Step 2: Commit**
```bash
git add docs/worklog.md
git commit -m "docs: update worklog with v3.5 changes"
```

---

## Task 9: Final Commit and Push

**Objective:** Ensure all changes are committed and ready for presentation.

**Step 1: Verify everything works**
```bash
cd backend && dotnet build
docker-compose up -d
```

**Step 2: Final commit if any stragglers**
```bash
git add -A
git commit -m "chore: final polish for school presentation"
```

**Step 3: Push to remote**
```bash
git push origin main
```

---

## Dependency Order

```
Task 1 (bug fix) ──┐
                   ├── independent, can run in parallel
Task 2 (bug fix) ──┘

Task 3 (news backend) ──┐
                        ├── sequential (page needs endpoint)
Task 4 (news page)    ──┘

Task 5 (nav link) ── depends on Task 4

Task 6 (compare backend) ──┐
                           ├── sequential
Task 7 (compare page)    ──┘

Task 8 (worklog) ── after all implementation
Task 9 (push)     ── final step
```
