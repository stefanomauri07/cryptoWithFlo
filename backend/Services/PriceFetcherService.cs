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
                _logger.LogError(ex, "Error fetching prices");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task FetchPricesAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var services = scope.ServiceProvider;

        var cryptos = await db.TrackedCryptos
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        if (cryptos.Count == 0)
        {
            _logger.LogWarning("No active cryptos to track");
            return;
        }

        var priceDtos = new List<CryptoPriceDto>();
        var historyEntries = new List<PriceHistory>();
        var now = DateTime.UtcNow;

        var binancePrices = new Dictionary<string, decimal>();
        try
        {
            var binance = services.GetRequiredService<BinanceService>();
            binancePrices = await binance.GetCurrentPricesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Binance fetch failed, will try CoinGecko as fallback");
        }

        JsonElement? coinGeckoJson = null;
        var apiKey = _configuration["COINGECKO_API_KEY"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                var ids = string.Join(",", cryptos.Select(c => c.Id));
                var cgUrl = $"https://api.coingecko.com/api/v3/simple/price?ids={ids}&vs_currencies=usd,eur&include_24hr_change=true&include_market_cap=true&include_24hr_vol=true";
                using var request = new HttpRequestMessage(HttpMethod.Get, cgUrl);
                request.Headers.Add("x-cg-pro-api-key", apiKey);

                var cgClient = _httpClientFactory.CreateClient("CoinGecko");
                var cgResponse = await cgClient.SendAsync(request, ct);

                if (cgResponse.IsSuccessStatusCode)
                {
                    coinGeckoJson = await cgResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                    _logger.LogInformation("CoinGecko data fetched successfully");
                }
                else
                {
                    _logger.LogWarning("CoinGecko returned {StatusCode}, using Binance-only data", (int)cgResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CoinGecko fetch failed, using Binance-only data");
            }
        }

        foreach (var crypto in cryptos)
        {
            var binanceSymbol = BinanceService.GetBinanceSymbol(crypto.Id);
            decimal priceUsd = 0;
            decimal? priceEur = null;
            decimal change24h = 0;
            decimal? marketCap = null;
            decimal? volume24h = null;
            decimal? allTimeHigh = null;

            if (coinGeckoJson.HasValue && coinGeckoJson.Value.TryGetProperty(crypto.Id, out var details))
            {
                priceUsd = Math.Round(details.GetProperty("usd").GetDecimal(), 8);
                priceEur = Math.Round(details.GetProperty("eur").GetDecimal(), 8);
                change24h = Math.Round(
                    details.TryGetProperty("usd_24h_change", out var change) ? change.GetDecimal() : 0m, 4);
                marketCap = details.TryGetProperty("usd_market_cap", out var mc) ? Math.Round(mc.GetDecimal(), 2) : null;
                volume24h = details.TryGetProperty("usd_24h_vol", out var vol) ? Math.Round(vol.GetDecimal(), 2) : null;
            }
            else if (binanceSymbol is not null && binancePrices.TryGetValue(binanceSymbol, out var bPrice))
            {
                priceUsd = Math.Round(bPrice, 8);
                priceEur = Math.Round(bPrice * 0.92m, 8);
                change24h = 0;
            }
            else
            {
                _logger.LogWarning("No price data for crypto {Id}", crypto.Id);
                continue;
            }

            priceDtos.Add(new CryptoPriceDto(crypto.Id, crypto.Name, crypto.Symbol, priceUsd,
                priceEur ?? priceUsd, change24h, marketCap, volume24h, allTimeHigh));

            historyEntries.Add(new PriceHistory
            {
                CryptoId = crypto.Id,
                PriceUsd = priceUsd,
                PriceEur = priceEur ?? priceUsd,
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

        _logger.LogInformation("Fetched prices for {Count} cryptos ({CoinGecko} CG, {Binance} Binance) in {Elapsed}ms",
            priceDtos.Count, coinGeckoJson.HasValue ? "with" : "without", binancePrices.Count > 0 ? "with" : "without", sw.ElapsedMilliseconds);
    }
}
