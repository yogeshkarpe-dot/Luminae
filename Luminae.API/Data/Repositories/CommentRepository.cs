using Dapper;
using Luminae.API.Models;

namespace Luminae.API.Data.Repositories;

public interface ICommentRepository
{
    Task<Comment?> AddAsync(Guid userId, Guid postId, string content);
    Task<IEnumerable<Comment>> GetByPostAsync(Guid postId, int offset, int limit);
    Task<bool> DeleteAsync(Guid commentId, Guid userId);
}

public class CommentRepository(IDbConnectionFactory db) : ICommentRepository
{
    public async Task<Comment?> AddAsync(Guid userId, Guid postId, string content)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Comment>(
            "SELECT * FROM sp_add_comment(@p_user_id, @p_post_id, @p_content)",
            new { p_user_id = userId, p_post_id = postId, p_content = content });
    }

    public async Task<IEnumerable<Comment>> GetByPostAsync(Guid postId, int offset, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Comment>(
            "SELECT * FROM sp_get_post_comments(@p_post_id, @p_offset, @p_limit)",
            new { p_post_id = postId, p_offset = offset, p_limit = limit });
    }

    public async Task<bool> DeleteAsync(Guid commentId, Guid userId)
    {
        using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT sp_delete_comment(@p_comment_id, @p_user_id)",
            new { p_comment_id = commentId, p_user_id = userId });
    }
}