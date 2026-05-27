using System.Text;
using CryptoApp.Data;
using CryptoApp.Endpoints;
using CryptoApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddMemoryCache();

var dbConfig = builder.Configuration.GetSection("Database");
var connStr = $"Server={dbConfig["Host"]};"
            + $"Port={dbConfig["Port"]};"
            + $"Database={dbConfig["Name"]};"
            + $"User={dbConfig["User"]};"
            + $"Password={dbConfig["Password"]};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

var binanceUrl = builder.Configuration["Binance:BaseUrl"] ?? "https://api.binance.com/";
builder.Services.AddHttpClient("Binance", client =>
{
    client.BaseAddress = new Uri(binanceUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoApp/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        RemoteCertificateValidationCallback = (_, _, _, _) => true
    }
});

builder.Services.AddSingleton<BinanceService>();
builder.Services.AddHostedService<PriceFetcherService>();
builder.Services.AddHostedService<AlertCheckerService>();

var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtConfig["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
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
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
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
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
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
