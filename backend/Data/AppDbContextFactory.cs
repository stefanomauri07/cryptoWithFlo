using CryptoApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace CryptoApp.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "3307";
        var db = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "crypto_tracker";
        var user = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "app_user";
        var pw = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "test123";

        var connStr = $"Server={host};Port={port};Database={db};User={user};Password={pw};";

        var serverVersion = new MariaDbServerVersion(new Version(11, 0, 0));

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connStr, serverVersion);

        return new AppDbContext(optionsBuilder.Options);
    }
}
