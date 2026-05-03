using Dapper;
using Luminae.API.Models;

namespace Luminae.API.Data.Repositories;

public interface ILikeRepository
{
    Task<LikeResult?> ToggleAsync(Guid userId, Guid postId);
}

public class LikeRepository(IDbConnectionFactory db) : ILikeRepository
{
    private readonly IDbConnectionFactory _db = db;

    public async Task<LikeResult?> ToggleAsync(Guid userId, Guid postId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<LikeResult>(
            "SELECT liked, like_count FROM sp_toggle_like(@p_user_id, @p_post_id)",
            new { p_user_id = userId, p_post_id = postId });
    }
}