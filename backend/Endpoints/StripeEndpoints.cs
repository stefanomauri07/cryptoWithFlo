using System.Security.Claims;
using CryptoApp.Data;
using CryptoApp.Models;
using CryptoApp.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace CryptoApp.Endpoints;

public static class StripeEndpoints
{
    public static RouteGroupBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscription");

        group.MapPost("/create-checkout", async (CheckoutRequest req, HttpContext http, AppDbContext db, StripeService stripe, ILogger<Program> logger) =>
        {
            try
            {
                var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var user = await db.Users.FindAsync(userId);
                if (user is null) return Results.NotFound(new { error = "User not found" });

                if (user.Role == "vip" || user.Role == "admin")
                    return Results.BadRequest(new { error = "Already subscribed" });

                var checkoutUrl = await stripe.CreateCheckoutSessionAsync(user, req.Plan);
                await db.SaveChangesAsync();

                return Results.Ok(new { url = checkoutUrl });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stripe checkout failed for user {UserId}", http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return Results.Problem(
                    title: "Stripe checkout failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        }).RequireAuthorization();

        group.MapGet("/status", async (HttpContext http, AppDbContext db) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await db.Users.FindAsync(userId);
            if (user is null) return Results.NotFound();

            var daysRemaining = user.SubscriptionEndDate.HasValue
                ? (int)(user.SubscriptionEndDate.Value - DateTime.UtcNow).TotalDays
                : (int?)null;

            return Results.Ok(new
            {
                isPro = user.Role == "vip" || user.Role == "admin",
                status = user.SubscriptionStatus,
                plan = user.SubscriptionPlan,
                endDate = user.SubscriptionEndDate,
                daysRemaining
            });
        }).RequireAuthorization();

        group.MapPost("/cancel", async (HttpContext http, AppDbContext db, StripeService stripe) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await db.Users.FindAsync(userId);
            if (user is null || user.Role != "vip")
                return Results.BadRequest(new { error = "No active subscription" });

            await stripe.CancelSubscriptionAsync(user);
            user.SubscriptionStatus = "cancelled";
            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization();

        group.MapPost("/finalize", async (FinalizeRequest req, AppDbContext db, ILogger<Program> logger) =>
        {
            try
            {
                var sessionService = new SessionService();
                var session = await sessionService.GetAsync(req.SessionId);

                if (session.PaymentStatus != "paid")
                    return Results.BadRequest(new { error = "Payment not completed" });

                if (!session.Metadata.TryGetValue("userId", out var userIdStr))
                    return Results.BadRequest(new { error = "Missing user metadata" });

                var userId = int.Parse(userIdStr);
                var user = await db.Users.FindAsync(userId);
                if (user is null) return Results.NotFound(new { error = "User not found" });

                if (user.Role == "vip" || user.Role == "admin")
                    return Results.Ok(new
                    {
                        isPro = true,
                        plan = user.SubscriptionPlan,
                        endDate = user.SubscriptionEndDate,
                        daysRemaining = user.SubscriptionEndDate.HasValue
                            ? (int)(user.SubscriptionEndDate.Value - DateTime.UtcNow).TotalDays
                            : (int?)null
                    });

                var plan = session.Metadata.TryGetValue("plan", out var planVal) ? planVal : "monthly";
                user.Role = "vip";
                user.StripeCustomerId = session.CustomerId;
                user.SubscriptionStatus = "active";
                user.SubscriptionPlan = plan;
                user.SubscriptionEndDate = plan switch
                {
                    "monthly"     => DateTime.UtcNow.AddMonths(1),
                    "semiannual"  => DateTime.UtcNow.AddMonths(6),
                    "annual"      => DateTime.UtcNow.AddMonths(12),
                    _             => DateTime.UtcNow.AddMonths(1)
                };
                await db.SaveChangesAsync();

                var daysRemaining = (int)(user.SubscriptionEndDate.Value - DateTime.UtcNow).TotalDays;

                return Results.Ok(new
                {
                    isPro = true,
                    plan = user.SubscriptionPlan,
                    endDate = user.SubscriptionEndDate,
                    daysRemaining
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to finalize subscription for session {SessionId}", req.SessionId);
                return Results.Problem(
                    title: "Finalize failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        });

        return group;
    }

    public static void MapStripeWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/stripe/webhook", async (HttpContext context, AppDbContext db, IConfiguration config) =>
        {
            var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var webhookSecret = config["Stripe:WebhookSecret"]!;

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    context.Request.Headers["Stripe-Signature"]!,
                    webhookSecret
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session?.Metadata != null && session.Metadata.TryGetValue("userId", out var userIdStr))
                    {
                        var userId = int.Parse(userIdStr);
                        var user = await db.Users.FindAsync(userId);
                        if (user != null)
                        {
                            var plan = session.Metadata.TryGetValue("plan", out var planVal) ? planVal : "monthly";
                            user.Role = "vip";
                            user.StripeCustomerId = session.CustomerId;
                            user.SubscriptionStatus = "active";
                            user.SubscriptionPlan = plan;
                            user.SubscriptionEndDate = plan switch
                            {
                                "monthly"     => DateTime.UtcNow.AddMonths(1),
                                "semiannual"  => DateTime.UtcNow.AddMonths(6),
                                "annual"      => DateTime.UtcNow.AddMonths(12),
                                _             => DateTime.UtcNow.AddMonths(1)
                            };
                            await db.SaveChangesAsync();
                        }
                    }
                }
                else if (stripeEvent.Type == "customer.subscription.deleted")
                {
                    var subscription = stripeEvent.Data.Object as Subscription;
                    var customerId = subscription!.CustomerId;
                    var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
                    if (user != null)
                    {
                        user.Role = "user";
                        user.SubscriptionStatus = null;
                        user.SubscriptionEndDate = null;
                        await db.SaveChangesAsync();
                    }
                }

                return Results.Ok();
            }
            catch (StripeException e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });
    }
}

public record CheckoutRequest(string Plan);
public record FinalizeRequest(string SessionId);
