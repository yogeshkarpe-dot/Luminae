using Dapper;
using Luminae.API.Models;

namespace Luminae.API.Data.Repositories;

public interface IMessageRepository
{
    Task<ConversationCreated?> GetOrCreateConversationAsync(Guid userA, Guid userB);
    Task<Message?> SendMessageAsync(Guid conversationId, Guid senderId, string content);
    Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, Guid userId, int offset, int limit);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId);
}

public class MessageRepository(IDbConnectionFactory db) : IMessageRepository
{
    public async Task<ConversationCreated?> GetOrCreateConversationAsync(Guid userA, Guid userB)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ConversationCreated>(
            "SELECT * FROM sp_get_or_create_conversation(@p_user_a, @p_user_b)",
            new { p_user_a = userA, p_user_b = userB });
    }

    public async Task<Message?> SendMessageAsync(Guid conversationId, Guid senderId, string content)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Message>(
            "SELECT * FROM sp_send_message(@p_conversation_id, @p_sender_id, @p_content)",
            new { p_conversation_id = conversationId, p_sender_id = senderId, p_content = content });
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, Guid userId, int offset, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Message>(
            "SELECT * FROM sp_get_conversation_messages(@p_conversation_id, @p_user_id, @p_offset, @p_limit)",
            new { p_conversation_id = conversationId, p_user_id = userId, p_offset = offset, p_limit = limit });
    }

    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(Guid userId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Conversation>(
            "SELECT * FROM sp_get_user_conversations(@p_user_id)",
            new { p_user_id = userId });
    }
}