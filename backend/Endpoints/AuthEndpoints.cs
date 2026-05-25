using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CryptoApp.Data;
using CryptoApp.Models;
using CryptoApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CryptoApp.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, AppDbContext db, BrevoEmailService brevo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || req.Password.Length < 6)
                return Results.BadRequest(new { error = "Invalid email or password too short (min 6 chars)" });

            var existing = await db.Users.AnyAsync(u => u.Email == req.Email);
            if (existing) return Results.Conflict(new { error = "Email already registered" });

            var user = new User
            {
                Email = req.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Role = "user",
                Name = req.Name ?? req.Email.Split('@')[0],
                IsVerified = false,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var code = Random.Shared.Next(100000, 999999).ToString();
            var otp = new Otp
            {
                Email = req.Email,
                Code = code,
                Purpose = "registration",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            db.Otps.Add(otp);
            await db.SaveChangesAsync();

            await brevo.SendOtpEmailAsync(req.Email, code, "registration");

            return Results.Created();
        });

        group.MapPost("/verify", async (VerifyOtpRequest req, AppDbContext db, IConfiguration config) =>
        {
            var otp = await db.Otps
                .Where(o => o.Email == req.Email && o.Purpose == "registration" && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp is null || otp.Code != req.Code)
                return Results.BadRequest(new { error = "Invalid or expired OTP" });

            otp.IsUsed = true;

            var user = await db.Users.FirstAsync(u => u.Email == req.Email);
            user.IsVerified = true;
            await db.SaveChangesAsync();

            var token = GenerateJwt(user, config);
            return Results.Ok(new { token, user = new { user.Id, user.Email, user.Role, user.Name } });
        });

        group.MapPost("/login", async (LoginRequest req, AppDbContext db, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            if (!user.IsVerified)
                return Results.Json(new { error = "Email not verified" }, statusCode: 403);

            var token = GenerateJwt(user, config);
            return Results.Ok(new { token, user = new { user.Id, user.Email, user.Role, user.Name } });
        });

        group.MapPost("/forgot-password", async (ForgotPasswordRequest req, AppDbContext db, BrevoEmailService brevo) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user is null) return Results.Ok();

            var code = Random.Shared.Next(100000, 999999).ToString();
            db.Otps.Add(new Otp
            {
                Email = req.Email,
                Code = code,
                Purpose = "reset",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            await brevo.SendOtpEmailAsync(req.Email, code, "reset");
            return Results.Ok();
        });

        group.MapPost("/reset-password", async (ResetPasswordRequest req, AppDbContext db) =>
        {
            if (req.NewPassword.Length < 6)
                return Results.BadRequest(new { error = "Password too short (min 6 chars)" });

            var otp = await db.Otps
                .Where(o => o.Email == req.Email && o.Purpose == "reset" && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp is null || otp.Code != req.Code)
                return Results.BadRequest(new { error = "Invalid or expired OTP" });

            otp.IsUsed = true;
            var user = await db.Users.FirstAsync(u => u.Email == req.Email);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapGet("/me", async (HttpContext http, AppDbContext db) =>
        {
            var userId = int.Parse(http.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await db.Users.FindAsync(userId);
            if (user is null) return Results.NotFound();
            return Results.Ok(new { user.Id, user.Email, user.Role, user.Name });
        }).RequireAuthorization();

        return group;
    }

    private static string GenerateJwt(User user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT_SECRET"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: "CryptoTracker",
            audience: "CryptoTracker",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(config.GetValue("JWT_EXPIRY_HOURS", 24)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(string Email, string Password, string Name);
public record VerifyOtpRequest(string Email, string Code);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
