namespace CryptoApp.Models;

public record PriceCacheEntry(
    List<CryptoPriceDto> Prices,
    DateTime Timestamp
);
