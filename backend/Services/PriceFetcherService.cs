using System.Diagnostics;
using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Services;

public class PriceFetcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PriceFetcherService> _logger;
    private readonly IConfiguration _configuration;

    public PriceFetcherService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<PriceFetcherService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
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
        var ticker24hr = new Dictionary<string, (decimal ChangePercent, decimal QuoteVolume)>();
        BinanceService? binanceSvc = null;
        try
        {
            binanceSvc = services.GetRequiredService<BinanceService>();
            binancePrices = await binanceSvc.GetCurrentPricesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Binance fetch failed");
        }

        try
        {
            if (binanceSvc is not null)
                ticker24hr = await binanceSvc.Get24hrTickerAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "24hr ticker fetch failed");
        }

        foreach (var crypto in cryptos)
        {
            var binanceSymbol = BinanceService.GetBinanceSymbol(crypto.Id);
            if (binanceSymbol is null || !binancePrices.TryGetValue(binanceSymbol, out var bPrice))
            {
                _logger.LogWarning("No price data for crypto {Id}", crypto.Id);
                continue;
            }

            var priceUsd = Math.Round(bPrice, 8);
            var priceEur = Math.Round(bPrice * 0.92m, 8);

            var change24h = 0m;
            var volume24h = (decimal?)null;

            if (binanceSymbol is not null && ticker24hr.TryGetValue(binanceSymbol, out var tickerData))
            {
                change24h = Math.Round(tickerData.ChangePercent, 4);
                volume24h = Math.Round(tickerData.QuoteVolume, 2);
            }

            priceDtos.Add(new CryptoPriceDto(crypto.Id, crypto.Name, crypto.Symbol, priceUsd,
                priceEur, change24h, null, volume24h, null));

            historyEntries.Add(new PriceHistory
            {
                CryptoId = crypto.Id,
                PriceUsd = priceUsd,
                PriceEur = priceEur,
                Change24hPercent = 0,
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

        _logger.LogInformation("Fetched prices for {Count} cryptos from Binance in {Elapsed}ms",
            priceDtos.Count, sw.ElapsedMilliseconds);
    }
}
