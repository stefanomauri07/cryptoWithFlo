using System.Security.Claims;
using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Endpoints;

public static class PortfolioEndpoints
{
    public static RouteGroupBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portfolio").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db, IMemoryCache cache, HttpContext http) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var holdings = await db.UserHoldings
                .Where(h => h.UserId == userId)
                .Include(h => h.Crypto)
                .ToListAsync();

            cache.TryGetValue("prices", out PriceCacheEntry? priceEntry);
            var prices = priceEntry?.Prices ?? new List<CryptoPriceDto>();

            var totalValueUsd = 0m;
            var totalValueEur = 0m;
            var holdingDtos = new List<PortfolioHoldingDto>();

            foreach (var h in holdings)
            {
                var price = prices.FirstOrDefault(p => p.Id == h.CryptoId);
                var valueUsd = price is not null ? h.Amount * price.PriceUsd : 0;
                var valueEur = price is not null ? h.Amount * price.PriceEur : 0;
                totalValueUsd += valueUsd;
                totalValueEur += valueEur;

                holdingDtos.Add(new PortfolioHoldingDto(
                    h.CryptoId,
                    h.Crypto.Name,
                    h.Crypto.Symbol,
                    h.Amount,
                    price?.PriceUsd,
                    price?.PriceEur,
                    valueUsd,
                    valueEur,
                    price?.Change24hPercent,
                    null
                ));
            }

            foreach (var dto in holdingDtos)
            {
                dto.AllocationPercent = totalValueUsd > 0 ? Math.Round((dto.ValueUsd ?? 0) / totalValueUsd * 100, 2) : 0;
            }

            return Results.Ok(new PortfolioResponseDto(holdingDtos, totalValueUsd, totalValueEur));
        });

        group.MapPost("/", async (UpsertHoldingRequest request, AppDbContext db, HttpContext http) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (request.Amount < 0)
                return Results.BadRequest(new { error = "Amount must be >= 0" });

            var cryptoExists = await db.TrackedCryptos.AnyAsync(c => c.Id == request.CryptoId);
            if (!cryptoExists)
                return Results.BadRequest(new { error = "Unknown crypto ID" });

            var existing = await db.UserHoldings
                .FirstOrDefaultAsync(h => h.UserId == userId && h.CryptoId == request.CryptoId);

            if (existing is not null)
            {
                if (request.Amount == 0)
                {
                    db.UserHoldings.Remove(existing);
                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }
                existing.Amount = request.Amount;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                if (request.Amount == 0)
                    return Results.Ok();
                db.UserHoldings.Add(new UserHolding
                {
                    UserId = userId,
                    CryptoId = request.CryptoId,
                    Amount = request.Amount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { cryptoId = request.CryptoId, amount = request.Amount });
        });

        group.MapDelete("/{cryptoId}", async (string cryptoId, AppDbContext db, HttpContext http) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var holding = await db.UserHoldings
                .FirstOrDefaultAsync(h => h.UserId == userId && h.CryptoId == cryptoId);

            if (holding is null)
                return Results.NotFound();

            db.UserHoldings.Remove(holding);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return group;
    }
}

public record UpsertHoldingRequest(string CryptoId, decimal Amount);

public class PortfolioHoldingDto
{
    public string CryptoId { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
    public decimal? PriceUsd { get; set; }
    public decimal? PriceEur { get; set; }
    public decimal? ValueUsd { get; set; }
    public decimal? ValueEur { get; set; }
    public decimal? Change24hPercent { get; set; }
    public decimal? AllocationPercent { get; set; }

    public PortfolioHoldingDto(string cryptoId, string name, string symbol, decimal amount,
        decimal? priceUsd, decimal? priceEur, decimal? valueUsd, decimal? valueEur,
        decimal? change24hPercent, decimal? allocationPercent)
    {
        CryptoId = cryptoId;
        Name = name;
        Symbol = symbol;
        Amount = amount;
        PriceUsd = priceUsd;
        PriceEur = priceEur;
        ValueUsd = valueUsd;
        ValueEur = valueEur;
        Change24hPercent = change24hPercent;
        AllocationPercent = allocationPercent;
    }
}

public record PortfolioResponseDto(
    List<PortfolioHoldingDto> Holdings,
    decimal TotalValueUsd,
    decimal TotalValueEur
);
