using System.Diagnostics;
using System.Text.Json;
using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Services;

public class PriceFetcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PriceFetcherService> _logger;
    private readonly IConfiguration _configuration;

    public PriceFetcherService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<PriceFetcherService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue("FETCH_INTERVAL_SECONDS", 120);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchPricesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prices from CoinGecko");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task FetchPricesAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cryptos = await db.TrackedCryptos
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        if (cryptos.Count == 0)
        {
            _logger.LogWarning("No active cryptos to track");
            return;
        }

        var ids = string.Join(",", cryptos.Select(c => c.Id));
        var url = $"https://api.coingecko.com/api/v3/simple/price?ids={ids}&vs_currencies=usd,eur&include_24hr_change=true&include_market_cap=true&include_24hr_vol=true";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var apiKey = _configuration["COINGECKO_API_KEY"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("x-cg-pro-api-key", apiKey);
        }

        var httpClient = _httpClientFactory.CreateClient("CoinGecko");
        var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("CoinGecko API returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(ct));
            return;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var priceDtos = new List<CryptoPriceDto>();
        var historyEntries = new List<PriceHistory>();
        var now = DateTime.UtcNow;

        foreach (var crypto in cryptos)
        {
            if (!json.TryGetProperty(crypto.Id, out var details))
            {
                _logger.LogWarning("No data returned for crypto {Id}", crypto.Id);
                continue;
            }

            var priceUsd = Math.Round(details.GetProperty("usd").GetDecimal(), 8);
            var priceEur = Math.Round(details.GetProperty("eur").GetDecimal(), 8);
            var change24h = Math.Round(
                details.TryGetProperty("usd_24h_change", out var change) ? change.GetDecimal() : 0m,
                4);

            priceDtos.Add(new CryptoPriceDto(crypto.Id, crypto.Name, crypto.Symbol, priceUsd, priceEur, change24h,
                details.TryGetProperty("usd_market_cap", out var mc) ? Math.Round(mc.GetDecimal(), 2) : null,
                details.TryGetProperty("usd_24h_vol", out var vol) ? Math.Round(vol.GetDecimal(), 2) : null,
                details.TryGetProperty("ath", out var ath) ? Math.Round(ath.GetDecimal(), 2) : null));

            historyEntries.Add(new PriceHistory
            {
                CryptoId = crypto.Id,
                PriceUsd = priceUsd,
                PriceEur = priceEur,
                Change24hPercent = change24h,
                RecordedAt = now
            });
        }

        var cacheEntry = new PriceCacheEntry(priceDtos, now);
        _cache.Set("prices", cacheEntry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
        });

        db.PriceHistories.AddRange(historyEntries);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Fetched prices for {Count} cryptos in {Elapsed}ms",
            priceDtos.Count, sw.ElapsedMilliseconds);
    }
}
