namespace CryptoApp.Models;

public record CryptoPriceDto(
    string Id,
    string Name,
    string Symbol,
    decimal PriceUsd,
    decimal PriceEur,
    decimal Change24hPercent,
    decimal? MarketCap = null,
    decimal? Volume24h = null,
    decimal? AllTimeHigh = null
);
