namespace Luminae.API.Models;

public class Comment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUsername { get; set; } = "";
    public string? AuthorDisplayName { get; set; }
    public string? AuthorAvatarUrl { get; set; }
}