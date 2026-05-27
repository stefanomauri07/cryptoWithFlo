using System.Security.Claims;
using CryptoApp.Data;
using CryptoApp.Models;
using CryptoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Endpoints;

public static class AdvisorEndpoints
{
    public static RouteGroupBuilder MapAdvisorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/advisor").RequireAuthorization();

        group.MapPost("/chat", async (ChatRequest req, HttpContext http, AppDbContext db, 
            IMemoryCache cache, OllamaService ollama) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Get portfolio
            var holdings = await db.UserHoldings
                .Where(h => h.UserId == userId)
                .Include(h => h.Crypto)
                .ToListAsync();

            // Get prices
            cache.TryGetValue("prices", out PriceCacheEntry? priceEntry);
            var prices = priceEntry?.Prices ?? new List<CryptoPriceDto>();

            // Build portfolio summary
            var portfolioLines = new List<string>();
            decimal totalValue = 0;
            foreach (var h in holdings)
            {
                var price = prices.FirstOrDefault(p => p.Id == h.CryptoId);
                var value = price != null ? h.Amount * price.PriceUsd : 0;
                totalValue += value;
                portfolioLines.Add($"- {h.Crypto.Name} ({h.Crypto.Symbol}): {h.Amount} units @ ${price?.PriceUsd ?? 0:F2} = ${value:F2}");
            }

            // Build market summary
            var marketLines = prices.Take(10).Select(p =>
                $"- {p.Name} ({p.Symbol}): ${p.PriceUsd:F2} ({p.Change24hPercent:+0.00;-0.00}%)").ToList();

            var portfolioText = holdings.Count > 0 
                ? "Portfolio:\n" + string.Join("\n", portfolioLines) + $"\nTotal: ${totalValue:F2}"
                : "Portfolio: Empty (no holdings yet)";

            var marketText = "Current Market:\n" + string.Join("\n", marketLines);

            var systemPrompt = $@"You are a professional cryptocurrency financial advisor AI. 
You analyze the user's portfolio and current market conditions to provide actionable insights.

Always structure your response with:
1. MARKET OVERVIEW: Brief assessment of current market conditions
2. PORTFOLIO ANALYSIS: Analysis of the user's holdings
3. DIRECTION: Your market direction prediction as a percentage (Bullish: X%, Bearish: Y%, Neutral: Z%)
4. RECOMMENDATION: One clear action the user should consider

Current data:
{marketText}

{portfolioText}

Keep responses concise and professional. Use percentages clearly.";

            var response = await ollama.ChatAsync(systemPrompt, req.Message);
            return Results.Ok(new { reply = response });
        });

        return group;
    }
}

public record ChatRequest(string Message);
