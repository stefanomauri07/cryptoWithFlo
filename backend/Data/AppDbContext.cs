using CryptoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TrackedCrypto> TrackedCryptos => Set<TrackedCrypto>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<Alert> Alerts => Set<Alert>();

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

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CryptoId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Condition).HasMaxLength(10).IsRequired();
            entity.Property(e => e.ThresholdUsd).HasPrecision(18, 8);
            entity.Property(e => e.WebhookUrl).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Crypto)
                  .WithMany(c => c.Alerts)
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
    }
}
