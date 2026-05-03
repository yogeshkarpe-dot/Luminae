namespace Luminae.API.Models;

public class Conversation
{
    public Guid ConversationId { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUsername { get; set; } = "";
    public string? OtherDisplayName { get; set; }
    public string? OtherAvatarUrl { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public long UnreadCount { get; set; }
}

public class Message
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = "";
    public string? SenderAvatarUrl { get; set; }
}

public class ConversationCreated
{
    public Guid Id { get; set; }
    public Guid ParticipantA { get; set; }
    public Guid ParticipantB { get; set; }
    public DateTime CreatedAt { get; set; }
}