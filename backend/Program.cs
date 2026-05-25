using System.Threading.Channels;
using CryptoApp.Data;
using CryptoApp.Endpoints;
using CryptoApp.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(Channel.CreateBounded<bool>(1));

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

builder.Services.AddHostedService<PriceFetcherService>();
builder.Services.AddHostedService<AlertCheckerService>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapOpenApi();

app.MapCryptoEndpoints();
app.MapAlertEndpoints();
app.MapHealthEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
