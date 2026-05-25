namespace CryptoApp.Models;

public class Alert
{
    public int Id { get; set; }
    public string CryptoId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal ThresholdUsd { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public bool IsTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TriggeredAt { get; set; }

    public TrackedCrypto Crypto { get; set; } = null!;
}
