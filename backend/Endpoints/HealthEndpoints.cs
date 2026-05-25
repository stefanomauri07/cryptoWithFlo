using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (AppDbContext db, IMemoryCache cache) =>
        {
            var dbStatus = "error";
            try
            {
                var canConnect = await db.Database.CanConnectAsync();
                dbStatus = canConnect ? "connected" : "error";
            }
            catch
            {
                dbStatus = "error";
            }

            var cacheAgeSeconds = -1;
            if (cache.TryGetValue("prices", out PriceCacheEntry? entry) && entry is not null)
            {
                cacheAgeSeconds = (int)(DateTime.UtcNow - entry.Timestamp).TotalSeconds;
            }

            return Results.Ok(new
            {
                status = "ok",
                db = dbStatus,
                cache_age_seconds = cacheAgeSeconds
            });
        });

        return app;
    }
}
