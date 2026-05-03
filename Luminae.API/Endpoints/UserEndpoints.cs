using Luminae.API.Data.Repositories;
using Luminae.API.Models;
using Luminae.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Luminae.API.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // GET /api/users/{username}
        group.MapGet("/{username}", async (string username, HttpContext ctx, IUserRepository users) =>
        {
            var viewerId = GetViewerId(ctx);
            var profile = await users.GetProfileAsync(username, viewerId);
            return profile == null
                ? Results.NotFound(ApiResponse<string>.Fail("User not found"))
                : Results.Ok(ApiResponse<UserProfile>.Ok(profile));
        });

        // GET /api/users/{username}/posts
        group.MapGet("/{username}/posts", async (string username, HttpContext ctx, IUserRepository users, IPostRepository posts, [FromQuery] int page = 1) =>
        {
            var viewerId = GetViewerId(ctx);
            var profile = await users.GetProfileAsync(username, viewerId);
            if (profile == null) return Results.NotFound();
            var userPosts = await posts.GetUserPostsAsync(profile.Id, viewerId, (page - 1) * 20, 20);
            return Results.Ok(ApiResponse<IEnumerable<UserPostSummary>>.Ok(userPosts));
        });

        // PUT /api/users/profile
        group.MapPut("/profile", [Authorize] async (UpdateProfileRequest req, HttpContext ctx, IUserRepository users) =>
        {
            var userId = GetUserId(ctx);
            var updated = await users.UpdateProfileAsync(userId, req.DisplayName, req.Bio, null, null, req.Website);
            return updated == null
                ? Results.BadRequest(ApiResponse<string>.Fail("Update failed"))
                : Results.Ok(ApiResponse<User>.Ok(updated));
        });

        // POST /api/users/avatar
        group.MapPost("/avatar", [Authorize] async (HttpContext ctx, IUserRepository users, IFileUploadService uploader) =>
        {
            var userId = GetUserId(ctx);
            var file = ctx.Request.Form.Files.GetFile("file");
            if (file == null || !uploader.IsValidMediaFile(file))
                return Results.BadRequest(ApiResponse<string>.Fail("Valid image required"));

            var (url, _) = await uploader.UploadAsync(file, "avatars");
            var updated = await users.UpdateProfileAsync(userId, null, null, url, null, null);
            return Results.Ok(ApiResponse<User>.Ok(updated!));
        }).DisableAntiforgery();

        // POST /api/users/{username}/follow
        group.MapPost("/{username}/follow", [Authorize] async (string username, HttpContext ctx, IUserRepository users, IFollowRepository follows) =>
        {
            var followerId = GetUserId(ctx);
            var target = await users.GetProfileAsync(username, null);
            if (target == null) return Results.NotFound();
            if (target.Id == followerId) return Results.BadRequest(ApiResponse<string>.Fail("Cannot follow yourself"));

            var result = await follows.ToggleFollowAsync(followerId, target.Id);
            return Results.Ok(ApiResponse<FollowResult>.Ok(result!));
        });

        // GET /api/users/{username}/followers
        group.MapGet("/{username}/followers", async (string username, IUserRepository users, IFollowRepository follows) =>
        {
            var profile = await users.GetProfileAsync(username, null);
            if (profile == null) return Results.NotFound();
            var list = await follows.GetFollowersAsync(profile.Id, 100);
            return Results.Ok(ApiResponse<IEnumerable<FollowUser>>.Ok(list));
        });

        // GET /api/users/{username}/following
        group.MapGet("/{username}/following", async (string username, IUserRepository users, IFollowRepository follows) =>
        {
            var profile = await users.GetProfileAsync(username, null);
            if (profile == null) return Results.NotFound();
            var list = await follows.GetFollowingAsync(profile.Id, 100);
            return Results.Ok(ApiResponse<IEnumerable<FollowUser>>.Ok(list));
        });

        // GET /api/users/search?q=
        group.MapGet("/search", async (IUserRepository users, [FromQuery] string q = "") =>
        {
            var results = await users.SearchUsersAsync(q);
            return Results.Ok(ApiResponse<IEnumerable<FollowUser>>.Ok(results));
        });
    }

    private static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static Guid? GetViewerId(HttpContext ctx)
    {
        var claim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }
}