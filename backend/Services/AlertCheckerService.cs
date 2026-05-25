using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Services;

public class AlertCheckerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlertCheckerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Channel<bool> _channel;

    public AlertCheckerService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<AlertCheckerService> logger,
        IConfiguration configuration,
        Channel<bool> channel)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _channel = channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue("ALERT_CHECK_INTERVAL_SECONDS", 120);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WaitAndCheckAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking alerts");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task WaitAndCheckAlertsAsync(CancellationToken ct)
    {
        try
        {
            await _channel.Reader.ReadAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Channel closed, skipping alert check");
            return;
        }

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
            .ToListAsync(ct);

        if (alerts.Count == 0) return;

        var httpClient = _httpClientFactory.CreateClient();
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

            var payload = new
            {
                crypto_id = alert.CryptoId,
                price_usd = currentPrice,
                threshold_usd = alert.ThresholdUsd,
                condition = alert.Condition,
                triggered_at = DateTime.UtcNow.ToString("o")
            };

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var webhookResponse = await httpClient.PostAsync(alert.WebhookUrl, content, ct);

                if (webhookResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Alert triggered: {CryptoId} {Condition} {Threshold} (price: {Price})",
                        alert.CryptoId, alert.Condition, alert.ThresholdUsd, currentPrice);
                }
                else
                {
                    _logger.LogWarning(
                        "Webhook call failed for alert {AlertId}: HTTP {StatusCode}",
                        alert.Id, (int)webhookResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook call failed for alert {AlertId}", alert.Id);
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
