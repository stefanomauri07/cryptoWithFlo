using System.ComponentModel.DataAnnotations;

namespace CryptoApp.Models;

public class TrackedCrypto
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
