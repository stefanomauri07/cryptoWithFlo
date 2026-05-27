using CryptoApp.Data;
using CryptoApp.Models;
using CryptoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Endpoints;

public static class CryptoEndpoints
{
    public static RouteGroupBuilder MapCryptoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crypto").RequireAuthorization();

        group.MapGet("/list", (IMemoryCache cache) =>
        {
            if (cache.TryGetValue("prices", out PriceCacheEntry? entry) && entry is not null)
            {
                return Results.Ok(entry.Prices);
            }

            return Results.Ok(Array.Empty<CryptoPriceDto>());
        });

        group.MapGet("/{id}/chart", async (string id, int? days, BinanceService binance, AppDbContext db, CancellationToken ct) =>
        {
            var lookback = days ?? 7;
            var data = await GetChartDataAsync(id, lookback, db, binance, ct);
            return Results.Ok(data);
        });

        group.MapGet("/compare/{crypto1}/{crypto2}", async (
            string crypto1, string crypto2, int? days,
            AppDbContext db, BinanceService binance, IMemoryCache cache,
            CancellationToken ct) =>
        {
            var d = days ?? 7;
            var data1 = await GetChartDataAsync(crypto1, d, db, binance, ct);
            var data2 = await GetChartDataAsync(crypto2, d, db, binance, ct);

            return Results.Ok(new
            {
                crypto1 = new { id = crypto1, data = data1 },
                crypto2 = new { id = crypto2, data = data2 }
            });
        });

        group.MapGet("/{id}/history", async (string id, DateTime? from, DateTime? to, AppDbContext db) =>
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
            var toDate = to ?? DateTime.UtcNow;

            var data = await db.PriceHistories
                .Where(p => p.CryptoId == id && p.RecordedAt >= fromDate && p.RecordedAt <= toDate)
                .OrderBy(p => p.RecordedAt)
                .Select(p => new
                {
                    timestamp = p.RecordedAt,
                    price_usd = p.PriceUsd,
                    price_eur = p.PriceEur,
                    change_24h_percent = p.Change24hPercent
                })
                .ToListAsync();

            return Results.Ok(data);
        });

        group.MapGet("/{id}/stats", (string id, IMemoryCache cache) =>
        {
            if (cache.TryGetValue("prices", out PriceCacheEntry? entry) && entry is not null)
            {
                var crypto = entry.Prices.FirstOrDefault(p => p.Id == id);
                if (crypto is not null)
                {
                    return Results.Ok(new
                    {
                        crypto.Id,
                        crypto.Name,
                        crypto.Symbol,
                        crypto.PriceUsd,
                        crypto.PriceEur,
                        crypto.Change24hPercent,
                        crypto.MarketCap,
                        crypto.Volume24h,
                        crypto.AllTimeHigh
                    });
                }
            }
            return Results.NotFound(new { error = "Crypto not found in cache" });
        });

        return group;
    }

    private static async Task<List<object>> GetChartDataAsync(
        string cryptoId, int days,
        AppDbContext db, BinanceService binance,
        CancellationToken ct)
    {
        var klines = await binance.GetKlinesAsync(cryptoId, days, ct);
        if (klines.Count > 0)
        {
            return klines.Select(k => (object)new { timestamp = k.Timestamp, price_usd = k.Close }).ToList();
        }

        var since = DateTime.UtcNow.AddDays(-days);
        var data = await db.PriceHistories
            .Where(p => p.CryptoId == cryptoId && p.RecordedAt >= since)
            .OrderBy(p => p.RecordedAt)
            .Select(p => new { timestamp = p.RecordedAt, price_usd = p.PriceUsd })
            .ToListAsync(ct);

        return data.Cast<object>().ToList();
    }
}
