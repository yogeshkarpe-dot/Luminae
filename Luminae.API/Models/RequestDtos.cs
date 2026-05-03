namespace Luminae.API.Models;

public record RegisterRequest(string Username, string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record UpdateProfileRequest(string? DisplayName, string? Bio, string? Website);
public record CreatePostRequest(string Title, string? Description, string MediaType, string[]? Tags);
public record AddCommentRequest(string Content);
public record SendMessageRequest(Guid RecipientId, string Content);
public record SendConversationMessageRequest(string Content);