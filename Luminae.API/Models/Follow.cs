namespace Luminae.API.Models;

public class FollowResult
{
    public bool IsFollowing { get; set; }
    public long FollowerCount { get; set; }
}

public class FollowUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}