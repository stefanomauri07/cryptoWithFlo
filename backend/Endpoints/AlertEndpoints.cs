using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoApp.Endpoints;

public static class AlertEndpoints
{
    public static RouteGroupBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alerts");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var alerts = await db.Alerts
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Results.Ok(alerts);
        });

        group.MapPost("/", async (CreateAlertRequest request, AppDbContext db) =>
        {
            if (request.Condition != "above" && request.Condition != "below")
            {
                return Results.BadRequest(new { error = "Condition must be 'above' or 'below'" });
            }

            var cryptoExists = await db.TrackedCryptos.AnyAsync(c => c.Id == request.CryptoId);
            if (!cryptoExists)
            {
                return Results.BadRequest(new { error = "Unknown crypto ID" });
            }

            var alert = new Alert
            {
                CryptoId = request.CryptoId,
                Condition = request.Condition,
                ThresholdUsd = request.ThresholdUsd,
                WebhookUrl = request.WebhookUrl,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            db.Alerts.Add(alert);
            await db.SaveChangesAsync();

            return Results.Created($"/api/alerts/{alert.Id}", alert);
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var alert = await db.Alerts.FindAsync(id);
            if (alert is null)
            {
                return Results.NotFound();
            }

            db.Alerts.Remove(alert);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return group;
    }
}

public record CreateAlertRequest(string CryptoId, string Condition, decimal ThresholdUsd, string WebhookUrl);
