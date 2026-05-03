using Luminae.API.Data.Repositories;
using Luminae.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Luminae.API.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/messages").WithTags("Messages").RequireAuthorization();

        // GET /api/messages  — all conversations
        group.MapGet("/", async (HttpContext ctx, IMessageRepository messages) =>
        {
            var userId = GetUserId(ctx);
            var convos = await messages.GetUserConversationsAsync(userId);
            return Results.Ok(ApiResponse<IEnumerable<Conversation>>.Ok(convos));
        });

        // POST /api/messages  — start conversation
        group.MapPost("/", async (SendMessageRequest req, HttpContext ctx, IMessageRepository messages) =>
        {
            var userId = GetUserId(ctx);
            var convo = await messages.GetOrCreateConversationAsync(userId, req.RecipientId);
            if (convo == null) return Results.BadRequest(ApiResponse<string>.Fail("Failed to create conversation"));

            var msg = await messages.SendMessageAsync(convo.Id, userId, req.Content);
            return Results.Ok(ApiResponse<Message>.Ok(msg!));
        });

        // GET /api/messages/{conversationId}
        group.MapGet("/{conversationId:guid}", async (Guid conversationId, HttpContext ctx, IMessageRepository messages, [FromQuery] int page = 1) =>
        {
            var userId = GetUserId(ctx);
            var msgs = await messages.GetMessagesAsync(conversationId, userId, (page - 1) * 50, 50);
            return Results.Ok(ApiResponse<IEnumerable<Message>>.Ok(msgs));
        });

        // POST /api/messages/{conversationId}
        group.MapPost("/{conversationId:guid}", async (Guid conversationId, SendConversationMessageRequest req, HttpContext ctx, IMessageRepository messages) =>
        {
            var userId = GetUserId(ctx);
            var msg = await messages.SendMessageAsync(conversationId, userId, req.Content);
            return msg == null
                ? Results.BadRequest(ApiResponse<string>.Fail("Failed to send message"))
                : Results.Ok(ApiResponse<Message>.Ok(msg));
        });
    }

    private static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}