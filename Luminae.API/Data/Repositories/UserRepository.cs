using Dapper;
using Luminae.API.Models;

namespace Luminae.API.Data.Repositories;

public interface IUserRepository
{
    Task<RegisterResult?> RegisterAsync(string username, string email, string passwordHash, string? displayName);
    Task<UserWithHash?> GetByEmailAsync(string email);
    Task<UserProfile?> GetProfileAsync(string username, Guid? viewerId);
    Task<User?> UpdateProfileAsync(Guid userId, string? displayName, string? bio, string? avatarUrl, string? bannerUrl, string? website);
    Task<IEnumerable<FollowUser>> SearchUsersAsync(string query, int limit = 10);
}

public class UserRepository(IDbConnectionFactory db) : IUserRepository
{
    public async Task<RegisterResult?> RegisterAsync(string username, string email, string passwordHash, string? displayName)
    {
        using var conn = db.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<RegisterResult>(
            "SELECT * FROM sp_register_user(@p_username, @p_email, @p_password_hash, @p_display_name)",
            new { p_username = username, p_email = email, p_password_hash = passwordHash, p_display_name = displayName });
        return result;
    }

    public async Task<UserWithHash?> GetByEmailAsync(string email)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserWithHash>(
            "SELECT * FROM sp_login_user(@p_email)",
            new { p_email = email });
    }

    public async Task<UserProfile?> GetProfileAsync(string username, Guid? viewerId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserProfile>(
            "SELECT * FROM sp_get_user_profile(@p_username, @p_viewer_id)",
            new { p_username = username, p_viewer_id = viewerId });
    }

    public async Task<User?> UpdateProfileAsync(Guid userId, string? displayName, string? bio, string? avatarUrl, string? bannerUrl, string? website)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM sp_update_user_profile(@p_user_id, @p_display_name, @p_bio, @p_avatar_url, @p_banner_url, @p_website)",
            new { p_user_id = userId, p_display_name = displayName, p_bio = bio, p_avatar_url = avatarUrl, p_banner_url = bannerUrl, p_website = website });
    }

    public async Task<IEnumerable<FollowUser>> SearchUsersAsync(string query, int limit = 10)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<FollowUser>(
            "SELECT * FROM sp_search_users(@p_query, @p_limit)",
            new { p_query = query, p_limit = limit });
    }
}