using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Services;

public class AlertCheckerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AlertCheckerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly BrevoEmailService _brevoEmailService;

    public AlertCheckerService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<AlertCheckerService> logger,
        IConfiguration configuration,
        BrevoEmailService brevoEmailService)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        _brevoEmailService = brevoEmailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue("Alerts:CheckIntervalSeconds", 120);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking alerts");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        if (!_cache.TryGetValue("prices", out PriceCacheEntry? cacheEntry) || cacheEntry is null)
        {
            _logger.LogDebug("No cached prices available for alert check");
            return;
        }

        var priceDict = cacheEntry.Prices.ToDictionary(p => p.Id, p => p.PriceUsd);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var alerts = await db.Alerts
            .Where(a => !a.IsTriggered)
            .Include(a => a.User)
            .ToListAsync(ct);

        if (alerts.Count == 0) return;

        var triggeredCount = 0;

        foreach (var alert in alerts)
        {
            if (!priceDict.TryGetValue(alert.CryptoId, out var currentPrice)) continue;

            var shouldTrigger = alert.Condition switch
            {
                "above" => currentPrice >= alert.ThresholdUsd,
                "below" => currentPrice <= alert.ThresholdUsd,
                _ => false
            };

            if (!shouldTrigger) continue;

            _logger.LogWarning(
                "Alert triggered: {CryptoId} {Condition} {Threshold} (price: {Price})",
                alert.CryptoId, alert.Condition, alert.ThresholdUsd, currentPrice);

            try
            {
                var symbol = alert.CryptoId switch
                {
                    "bitcoin" => "BTC",
                    "ethereum" => "ETH",
                    "cardano" => "ADA",
                    "dogecoin" => "DOGE",
                    "solana" => "SOL",
                    "ripple" => "XRP",
                    _ => alert.CryptoId.ToUpper()
                };

                if (alert.User != null)
                {
                    await _brevoEmailService.SendAlertEmailAsync(
                        alert.User.Email,
                        alert.CryptoId,
                        symbol,
                        currentPrice,
                        alert.ThresholdUsd,
                        alert.Condition);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send alert email for alert {AlertId}", alert.Id);
            }

            alert.IsTriggered = true;
            alert.TriggeredAt = DateTime.UtcNow;
            triggeredCount++;
        }

        if (triggeredCount > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Triggered {Count} alert(s)", triggeredCount);
        }
    }
}
