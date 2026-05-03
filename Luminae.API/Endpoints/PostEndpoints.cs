using Luminae.API.Data.Repositories;
using Luminae.API.Handlers;
using Luminae.API.Models;
using Luminae.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Luminae.API.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/posts").WithTags("Posts");

        // GET /api/posts  — public feed
        group.MapGet("/", async (
            HttpContext ctx,
            IPostRepository posts,
            [FromQuery] string? mediaType,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20) =>
        {
            var viewerId = GetViewerId(ctx);
            var offset = (page - 1) * size;
            var feed = await posts.GetFeedAsync(viewerId, mediaType, offset, size);
            return Results.Ok(ApiResponse<IEnumerable<PostSummary>>.Ok(feed));
        });

        // GET /api/posts/search
        group.MapGet("/search", async (IPostRepository posts, [FromQuery] string q = "", [FromQuery] int page = 1) =>
        {
            var results = await posts.SearchAsync(q, (page - 1) * 20, 20);
            return Results.Ok(ApiResponse<IEnumerable<PostSummary>>.Ok(results));
        });

        // GET /api/posts/{id}
        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, IPostRepository posts) =>
        {
            var viewerId = GetViewerId(ctx);
            var post = await posts.GetByIdAsync(id, viewerId);
            return post == null
                ? Results.NotFound(ApiResponse<string>.Fail("Post not found"))
                : Results.Ok(ApiResponse<PostSummary>.Ok(post));
        });

        // POST /api/posts  — multipart form: file + metadata
        group.MapPost("/", [Authorize] async (
            HttpContext ctx,
            IPostRepository posts,
            IFileUploadService uploader,
            [FromForm] string title,
            [FromForm] string? description,
            [FromForm] string mediaType,
            [FromForm] string? tags) =>
        {
            var userId = GetUserId(ctx);
            var file = ctx.Request.Form.Files.GetFile("file");

            if (file == null || !uploader.IsValidMediaFile(file))
                return Results.BadRequest(ApiResponse<string>.Fail("Valid media file required (jpg, png, gif, webp, mp4, webm)"));

            var (url, thumbUrl) = await uploader.UploadAsync(file, "posts");
            var tagArray = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            var post = await posts.CreateAsync(userId, title, description, url, thumbUrl, mediaType, tagArray);
            return post == null
                ? Results.BadRequest(ApiResponse<string>.Fail("Failed to create post"))
                : Results.Created($"/api/posts/{post.Id}", ApiResponse<PostCreated>.Ok(post));
        }).DisableAntiforgery();

        // DELETE /api/posts/{id}
        group.MapDelete("/{id:guid}", [Authorize] async (Guid id, HttpContext ctx, IPostRepository posts) =>
        {
            var userId = GetUserId(ctx);
            var result = await posts.DeleteAsync(id, userId);
            return result.Success
                ? Results.Ok(ApiResponse<string>.Ok("Deleted"))
                : Results.BadRequest(ApiResponse<string>.Fail(result.Message));
        });

        // POST /api/posts/{id}/like  — toggle like
        group.MapPost("/{id:guid}/like", [Authorize] async (Guid id, HttpContext ctx, ICommentRepository _, IFollowRepository __, IPostRepository ___, IMessageRepository ____, LikeToggleHandler handler) =>
            await handler.ToggleAsync(id, ctx));

        // GET /api/posts/{id}/comments
        group.MapGet("/{id:guid}/comments", async (Guid id, ICommentRepository comments, [FromQuery] int page = 1) =>
        {
            var result = await comments.GetByPostAsync(id, (page - 1) * 30, 30);
            return Results.Ok(ApiResponse<IEnumerable<Comment>>.Ok(result));
        });

        // POST /api/posts/{id}/comments
        group.MapPost("/{id:guid}/comments", [Authorize] async (Guid id, AddCommentRequest req, HttpContext ctx, ICommentRepository comments) =>
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Results.BadRequest(ApiResponse<string>.Fail("Comment cannot be empty"));

            var userId = GetUserId(ctx);
            var comment = await comments.AddAsync(userId, id, req.Content);
            return comment == null
                ? Results.BadRequest(ApiResponse<string>.Fail("Failed to add comment"))
                : Results.Ok(ApiResponse<Comment>.Ok(comment));
        });

        // DELETE /api/posts/{postId}/comments/{commentId}
        group.MapDelete("/{postId:guid}/comments/{commentId:guid}", [Authorize] async (Guid commentId, HttpContext ctx, ICommentRepository comments) =>
        {
            var userId = GetUserId(ctx);
            var ok = await comments.DeleteAsync(commentId, userId);
            return ok ? Results.Ok() : Results.NotFound();
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