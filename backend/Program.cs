using System.Text;
using CryptoApp.Data;
using CryptoApp.Endpoints;
using CryptoApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath)) DotNetEnv.Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("CoinGecko", client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoApp/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        RemoteCertificateValidationCallback = (_, _, _, _) => true
    }
});

var connStr = $"Server={builder.Configuration["DATABASE_HOST"]};"
            + $"Port={builder.Configuration["DATABASE_PORT"]};"
            + $"Database={builder.Configuration["DATABASE_NAME"]};"
            + $"User={builder.Configuration["DATABASE_USER"]};"
            + $"Password={builder.Configuration["DATABASE_PASSWORD"]};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

builder.Services.AddHttpClient("Binance", client =>
{
    client.BaseAddress = new Uri("https://api.binance.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoApp/1.0");
});

builder.Services.AddSingleton<BinanceService>();
builder.Services.AddHostedService<PriceFetcherService>();
builder.Services.AddHostedService<AlertCheckerService>();

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not set");
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "CryptoTracker",
            ValidAudience = "CryptoTracker",
            IssuerSigningKey = jwtKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

builder.Services.AddSingleton<BrevoEmailService>();
builder.Services.AddSingleton<StripeService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(feature?.Error, "Unhandled exception");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
    });
});

app.UseCors();

app.MapStripeWebhook();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapOpenApi();

app.MapCryptoEndpoints();
app.MapAlertEndpoints();
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapPortfolioEndpoints();
app.MapSubscriptionEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
