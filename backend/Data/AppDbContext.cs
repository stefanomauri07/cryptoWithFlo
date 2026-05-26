using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TrackedCrypto> TrackedCryptos => Set<TrackedCrypto>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Otp> Otps => Set<Otp>();
    public DbSet<UserHolding> UserHoldings => Set<UserHolding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrackedCrypto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Symbol).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CryptoId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PriceUsd).HasPrecision(18, 8);
            entity.Property(e => e.PriceEur).HasPrecision(18, 8);
            entity.Property(e => e.Change24hPercent).HasPrecision(10, 4);

            entity.HasIndex(e => new { e.CryptoId, e.RecordedAt });

            entity.HasOne(e => e.Crypto)
                  .WithMany(c => c.PriceHistories)
                  .HasForeignKey(e => e.CryptoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(255);
            entity.Property(e => e.SubscriptionStatus).HasMaxLength(50);
        });

        modelBuilder.Entity<Otp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Email, e.Purpose, e.IsUsed });
            entity.Property(e => e.Code).HasMaxLength(6).IsRequired();
            entity.Property(e => e.Purpose).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CryptoId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Condition).HasMaxLength(10).IsRequired();
            entity.Property(e => e.ThresholdUsd).HasPrecision(18, 8);

            entity.HasOne(e => e.Crypto)
                  .WithMany(c => c.Alerts)
                  .HasForeignKey(e => e.CryptoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Alerts)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserHolding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CryptoId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 8).IsRequired();

            entity.HasIndex(e => new { e.UserId, e.CryptoId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Holdings)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Crypto)
                  .WithMany(c => c.Holdings)
                  .HasForeignKey(e => e.CryptoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrackedCrypto>().HasData(
            new TrackedCrypto { Id = "bitcoin",  Name = "Bitcoin",  Symbol = "BTC",  IsActive = true },
            new TrackedCrypto { Id = "ethereum", Name = "Ethereum", Symbol = "ETH",  IsActive = true },
            new TrackedCrypto { Id = "cardano",  Name = "Cardano",  Symbol = "ADA",  IsActive = true },
            new TrackedCrypto { Id = "dogecoin", Name = "Dogecoin", Symbol = "DOGE", IsActive = true },
            new TrackedCrypto { Id = "solana",   Name = "Solana",   Symbol = "SOL",  IsActive = true },
            new TrackedCrypto { Id = "ripple",   Name = "Ripple",   Symbol = "XRP",  IsActive = true }
        );

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Email = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            Role = "admin",
            Name = "Admin",
            IsVerified = true,
            CreatedAt = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<UserHolding>().HasData(
            new UserHolding { Id = 1, UserId = 1, CryptoId = "bitcoin",  Amount = 0m, CreatedAt = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc) },
            new UserHolding { Id = 2, UserId = 1, CryptoId = "ethereum", Amount = 0m, CreatedAt = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc) },
            new UserHolding { Id = 3, UserId = 1, CryptoId = "solana",   Amount = 0m, CreatedAt = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
