using CryptoApp.Data;
using CryptoApp.Models;
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

        group.MapGet("/{id}/chart", async (string id, int? days, AppDbContext db) =>
        {
            var lookback = days ?? 7;
            var since = DateTime.UtcNow.AddDays(-lookback);

            var data = await db.PriceHistories
                .Where(p => p.CryptoId == id && p.RecordedAt >= since)
                .OrderBy(p => p.RecordedAt)
                .Select(p => new { timestamp = p.RecordedAt, price_usd = p.PriceUsd })
                .ToListAsync();

            return Results.Ok(data);
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

        return group;
    }
}
