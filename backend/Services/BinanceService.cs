using System.Diagnostics;
using System.Text.Json;
using CryptoApp.Data;
using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoApp.Services;

public class BinanceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BinanceService> _logger;

    private static readonly Dictionary<string, string> SymbolMap = new()
    {
        { "bitcoin", "BTCUSDT" },
        { "ethereum", "ETHUSDT" },
        { "cardano", "ADAUSDT" },
        { "dogecoin", "DOGEUSDT" },
        { "solana", "SOLUSDT" },
        { "ripple", "XRPUSDT" }
    };

    public BinanceService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<BinanceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public static string? GetBinanceSymbol(string cryptoId)
    {
        return SymbolMap.TryGetValue(cryptoId, out var symbol) ? symbol : null;
    }

    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(CancellationToken ct = default)
    {
        var symbols = string.Join("],[", SymbolMap.Values.Select(s => $"\"{s}\""));
        var url = $"/api/v3/ticker/price?symbols=[{symbols}]";

        try
        {
            var client = _httpClientFactory.CreateClient("Binance");
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Binance ticker returned {StatusCode}", (int)response.StatusCode);
                return new Dictionary<string, decimal>();
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var result = new Dictionary<string, decimal>();

            foreach (var item in json.EnumerateArray())
            {
                var symbol = item.GetProperty("symbol").GetString()!;
                var price = decimal.Parse(item.GetProperty("price").GetString()!);
                result[symbol] = price;
            }

            _logger.LogInformation("Fetched {Count} prices from Binance", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prices from Binance");
            return new Dictionary<string, decimal>();
        }
    }

    public async Task<List<BinanceKline>> GetKlinesAsync(string cryptoId, int days, CancellationToken ct = default)
    {
        var symbol = GetBinanceSymbol(cryptoId);
        if (symbol is null)
            return new List<BinanceKline>();

        var (interval, limit) = days switch
        {
            <= 1 => ("15m", 96),
            <= 7 => ("1h", 168),
            <= 30 => ("4h", 180),
            _ => ("1d", Math.Min(days, 365))
        };

        var cacheKey = $"binance_klines_{cryptoId}_{days}";
        if (_cache.TryGetValue(cacheKey, out List<BinanceKline>? cached) && cached is not null)
            return cached;

        try
        {
            var client = _httpClientFactory.CreateClient("Binance");
            var url = $"/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Binance klines returned {StatusCode} for {Symbol}", (int)response.StatusCode, symbol);
                return new List<BinanceKline>();
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var klines = new List<BinanceKline>();

            foreach (var candle in json.EnumerateArray())
            {
                klines.Add(new BinanceKline(
                    DateTimeOffset.FromUnixTimeMilliseconds(candle[0].GetInt64()).UtcDateTime,
                    decimal.Parse(candle[1].GetString()!),
                    decimal.Parse(candle[2].GetString()!),
                    decimal.Parse(candle[3].GetString()!),
                    decimal.Parse(candle[4].GetString()!),
                    decimal.Parse(candle[5].GetString()!)
                ));
            }

            _cache.Set(cacheKey, klines, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            });

            _logger.LogInformation("Fetched {Count} klines from Binance for {Symbol}", klines.Count, symbol);
            return klines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching klines from Binance for {Symbol}", symbol);
            return new List<BinanceKline>();
        }
    }

    public static string? GetCryptoIdFromSymbol(string symbol)
    {
        return SymbolMap.FirstOrDefault(kv => kv.Value == symbol).Key;
    }
}

public record BinanceKline(
    DateTime Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume
);
