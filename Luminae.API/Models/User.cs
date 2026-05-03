namespace Luminae.API.Models;

// ─────────────────────────────────────────
//  USER MODELS
// ─────────────────────────────────────────
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserWithHash : User
{
    public string PasswordHash { get; set; } = "";
}

public class UserProfile : User
{
    public long PostCount { get; set; }
    public long FollowerCount { get; set; }
    public long FollowingCount { get; set; }
    public bool IsFollowing { get; set; }
}