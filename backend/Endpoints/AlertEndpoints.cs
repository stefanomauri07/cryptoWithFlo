using System.Security.Claims;
using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoApp.Endpoints;

public static class AlertEndpoints
{
    public static RouteGroupBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alerts").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db, HttpContext http, string? status, string? q) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = http.User.FindFirst(ClaimTypes.Role)?.Value;

            var query = db.Alerts.AsQueryable();

            if (role != "admin")
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    query = query.Where(a => !a.IsTriggered);
                else if (status == "triggered")
                    query = query.Where(a => a.IsTriggered);
            }

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(a => a.CryptoId.Contains(q) || a.Condition.Contains(q));
            }

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.CryptoId,
                    a.Condition,
                    a.ThresholdUsd,
                    a.IsTriggered,
                    a.CreatedAt,
                    a.TriggeredAt,
                    a.UserId,
                    UserEmail = a.User.Email
                })
                .ToListAsync();

            return Results.Ok(alerts);
        });

        group.MapPost("/", async (CreateAlertRequest request, AppDbContext db, HttpContext http) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

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
                UserId = userId,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            db.Alerts.Add(alert);
            await db.SaveChangesAsync();

            return Results.Created($"/api/alerts/{alert.Id}", new
            {
                alert.Id,
                alert.CryptoId,
                alert.Condition,
                alert.ThresholdUsd,
                alert.IsTriggered,
                alert.CreatedAt,
                alert.TriggeredAt,
                alert.UserId
            });
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db, HttpContext http) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = http.User.FindFirst(ClaimTypes.Role)?.Value;

            var alert = await db.Alerts.FindAsync(id);
            if (alert is null)
            {
                return Results.NotFound();
            }

            if (role != "admin" && alert.UserId != userId)
            {
                return Results.Forbid();
            }

            db.Alerts.Remove(alert);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return group;
    }
}

public record CreateAlertRequest(string CryptoId, string Condition, decimal ThresholdUsd);
