using Dapper;
using Luminae.API.Models;

namespace Luminae.API.Data.Repositories;

public interface IFollowRepository
{
    Task<FollowResult?> ToggleFollowAsync(Guid followerId, Guid followingId);
    Task<IEnumerable<FollowUser>> GetFollowersAsync(Guid userId, int limit);
    Task<IEnumerable<FollowUser>> GetFollowingAsync(Guid userId, int limit);
}

public class FollowRepository(IDbConnectionFactory db) : IFollowRepository
{
    public async Task<FollowResult?> ToggleFollowAsync(Guid followerId, Guid followingId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FollowResult>(
            "SELECT is_following, follower_count FROM sp_toggle_follow(@p_follower_id, @p_following_id)",
            new { p_follower_id = followerId, p_following_id = followingId });
    }

    public async Task<IEnumerable<FollowUser>> GetFollowersAsync(Guid userId, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<FollowUser>(
            "SELECT * FROM sp_get_followers(@p_user_id, @p_limit)",
            new { p_user_id = userId, p_limit = limit });
    }

    public async Task<IEnumerable<FollowUser>> GetFollowingAsync(Guid userId, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<FollowUser>(
            "SELECT * FROM sp_get_following(@p_user_id, @p_limit)",
            new { p_user_id = userId, p_limit = limit });
    }
}