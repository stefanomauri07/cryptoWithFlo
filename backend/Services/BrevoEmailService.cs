using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CryptoApp.Services;

public class BrevoEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly HttpClient _httpClient;

    public BrevoEmailService(IConfiguration configuration, ILogger<BrevoEmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode, string purpose)
    {
        var title = purpose == "registration" ? "Verify your email" : "Reset your password";
        var body = purpose == "registration"
            ? "Use this code to complete your registration."
            : "Use this code to reset your password.";

        var html = $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/></head>
<body style=""margin:0;padding:0;background:#0e1322;font-family:Inter,Arial,sans-serif;"">
<div style=""max-width:480px;margin:0 auto;background:#0e1322;color:#dee1f7;border-radius:16px;overflow:hidden;border:1px solid #2a2d3e;"">
<div style=""padding:32px;background:#161b2b;"">
<h1 style=""color:#00ff88;font-size:24px;margin:0 0 8px;font-family:Inter,sans-serif;"">CryptoTracker</h1>
<p style=""color:#8b8fa3;margin:0;font-size:13px;text-transform:uppercase;letter-spacing:0.05em;"">Secure Verification</p>
</div>
<div style=""padding:32px;"">
<h2 style=""margin:0 0 16px;font-size:20px;font-family:Inter,sans-serif;font-weight:600;color:#dee1f7;"">{title}</h2>
<p style=""color:#8b8fa3;margin:0 0 24px;font-size:15px;"">{body}</p>
<div style=""background:#1a1f2f;border-radius:12px;padding:28px 24px;text-align:center;margin-bottom:24px;"">
<div style=""font-size:36px;font-weight:700;letter-spacing:12px;color:#00ff88;font-family:monospace;"">{otpCode}</div>
</div>
<p style=""color:#8b8fa3;font-size:13px;margin:0;"">Code expires in 10 minutes. If you didn't request this, please ignore this email.</p>
</div>
</div>
</body>
</html>";

        await SendEmailAsync(toEmail, title, html);
    }

    public async Task SendAlertEmailAsync(string toEmail, string cryptoId, string symbol, decimal priceUsd, decimal thresholdUsd, string condition)
    {
        var conditionText = condition == "above" ? "Above" : "Below";
        var changeColor = condition == "above" ? "#00ff88" : "#ff4757";

        var html = $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/></head>
<body style=""margin:0;padding:0;background:#0e1322;font-family:Inter,Arial,sans-serif;"">
<div style=""max-width:480px;margin:0 auto;background:#0e1322;color:#dee1f7;border-radius:16px;overflow:hidden;border:1px solid #2a2d3e;"">
<div style=""padding:32px;background:#161b2b;"">
<h1 style=""color:#00ff88;font-size:24px;margin:0 0 8px;font-family:Inter,sans-serif;"">CryptoTracker</h1>
<p style=""color:#8b8fa3;margin:0;font-size:13px;text-transform:uppercase;letter-spacing:0.05em;"">Price Alert</p>
</div>
<div style=""padding:32px;"">
<div style=""text-align:center;margin-bottom:24px;"">
<span style=""font-size:48px;"">🚨</span>
</div>
<h2 style=""margin:0 0 8px;font-size:20px;font-family:Inter,sans-serif;font-weight:600;text-align:center;color:#dee1f7;"">{symbol} Alert Triggered</h2>
<p style=""color:#8b8fa3;text-align:center;margin:0 0 24px;font-size:14px;"">Your price alert has been triggered</p>
<div style=""background:#1a1f2f;border-radius:12px;padding:24px;margin-bottom:24px;"">
<table style=""width:100%;border-collapse:collapse;"">
<tr><td style=""padding:8px 0;color:#8b8fa3;font-size:14px;"">Current Price</td><td style=""padding:8px 0;text-align:right;font-size:24px;font-weight:700;color:{changeColor};font-family:monospace;"">${priceUsd:N2}</td></tr>
<tr><td style=""padding:8px 0;color:#8b8fa3;font-size:14px;"">Condition</td><td style=""padding:8px 0;text-align:right;font-size:14px;font-weight:600;color:#dee1f7;"">Price {conditionText} ${thresholdUsd:N2}</td></tr>
<tr><td style=""padding:8px 0;color:#8b8fa3;font-size:14px;"">Triggered at</td><td style=""padding:8px 0;text-align:right;font-size:13px;color:#8b8fa3;"">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
</table>
</div>
<div style=""text-align:center;"">
<a href=""http://localhost:3000"" style=""display:inline-block;background:#00ff88;color:#003919;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;font-size:15px;font-family:Inter,sans-serif;"">View Dashboard</a>
</div>
</div>
</div>
</body>
</html>";

        var subject = $"🚨 {symbol} Price Alert - ${priceUsd:N2} ({conditionText} ${thresholdUsd:N2})";
        await SendEmailAsync(toEmail, subject, html);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var apiKey = _configuration["Brevo:ApiKey"];
        var sender = _configuration["Brevo:Sender"] ?? "mauristefano1@gmail.com";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Brevo:ApiKey not configured");
            return;
        }

        var payload = new
        {
            sender = new { email = sender, name = "CryptoTracker" },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Brevo API error {Status}: {Body}", (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}
