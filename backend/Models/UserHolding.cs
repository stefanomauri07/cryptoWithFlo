namespace CryptoApp.Models;

public class UserHolding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CryptoId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public TrackedCrypto Crypto { get; set; } = null!;
}
