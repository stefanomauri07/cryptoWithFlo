namespace CryptoApp.Models;

public record CryptoPriceDto(
    string Id,
    string Name,
    string Symbol,
    decimal PriceUsd,
    decimal PriceEur,
    decimal Change24hPercent
);
