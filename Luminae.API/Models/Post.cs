namespace Luminae.API.Models;


public class PostSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string MediaUrl { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = "";
    public string[]? Tags { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Author
    public Guid AuthorId { get; set; }
    public string AuthorUsername { get; set; } = "";
    public string? AuthorDisplayName { get; set; }
    public string? AuthorAvatarUrl { get; set; }

    // Engagement
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public bool IsLiked { get; set; }
}

public class UserPostSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string MediaUrl { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = "";
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public bool IsLiked { get; set; }
}

public class PostCreated
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string MediaUrl { get; set; } = "";
    public string MediaType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}