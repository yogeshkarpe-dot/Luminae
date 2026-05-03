using Luminae.API.Data.Repositories;
using Luminae.API.Models;
using Luminae.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Luminae.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // POST /api/auth/register
        group.MapPost("/register", async (RegisterRequest req, IUserRepository users, ITokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(ApiResponse<string>.Fail("Username, email and password are required"));

            if (req.Password.Length < 6)
                return Results.BadRequest(ApiResponse<string>.Fail("Password must be at least 6 characters"));

            var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            var result = await users.RegisterAsync(req.Username, req.Email, hash, req.DisplayName);

            if (result == null || result.Id == null)
                return Results.BadRequest(ApiResponse<string>.Fail(result?.ErrorMessage ?? "Registration failed"));

            var user = new User
            {
                Id = result.Id.Value,
                Username = result.Username!,
                Email = result.Email!,
                DisplayName = result.DisplayName,
                CreatedAt = result.CreatedAt ?? DateTime.UtcNow
            };

            return Results.Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = tokens.GenerateToken(user),
                User = user
            }));
        });

        // POST /api/auth/login
        group.MapPost("/login", async (LoginRequest req, IUserRepository users, ITokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(ApiResponse<string>.Fail("Email and password are required"));

            var user = await users.GetByEmailAsync(req.Email);
            if (user == null || !user.IsActive)
                return Results.Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            return Results.Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
            {
                Token = tokens.GenerateToken(user),
                User = user
            }));
        });

        // GET /api/auth/me
        group.MapGet("/me", [Authorize] async (ClaimsPrincipal principal, IUserRepository users) =>
        {
            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var username = principal.FindFirstValue("username")!;
            var profile = await users.GetProfileAsync(username, userId);
            return profile == null ? Results.NotFound() : Results.Ok(ApiResponse<UserProfile>.Ok(profile));
        });
    }
}