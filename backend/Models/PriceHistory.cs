namespace CryptoApp.Models;

public class PriceHistory
{
    public int Id { get; set; }
    public string CryptoId { get; set; } = string.Empty;
    public decimal PriceUsd { get; set; }
    public decimal PriceEur { get; set; }
    public decimal Change24hPercent { get; set; }
    public DateTime RecordedAt { get; set; }

    public TrackedCrypto Crypto { get; set; } = null!;
}
