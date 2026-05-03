using Dapper;
using Luminae.API.Models;

namespace Luminae.API.Data.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<PostSummary>> GetFeedAsync(Guid? viewerId, string? mediaType, int offset, int limit);
    Task<PostSummary?> GetByIdAsync(Guid postId, Guid? viewerId);
    Task<IEnumerable<UserPostSummary>> GetUserPostsAsync(Guid profileUserId, Guid? viewerId, int offset, int limit);
    Task<PostCreated?> CreateAsync(Guid userId, string title, string? description, string? mediaUrl, string? thumbnailUrl, string mediaType, string[]? tags);
    Task<OperationResult> DeleteAsync(Guid postId, Guid userId);
    Task<IEnumerable<PostSummary>> SearchAsync(string query, int offset, int limit);
}

public class PostRepository(IDbConnectionFactory db) : IPostRepository
{
    public async Task<IEnumerable<PostSummary>> GetFeedAsync(Guid? viewerId, string? mediaType, int offset, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<PostSummary>(
            "SELECT * FROM sp_get_feed(@p_viewer_id, @p_media_type, @p_offset, @p_limit)",
            new { p_viewer_id = viewerId, p_media_type = mediaType, p_offset = offset, p_limit = limit });
    }

    public async Task<PostSummary?> GetByIdAsync(Guid postId, Guid? viewerId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PostSummary>(
            "SELECT * FROM sp_get_post_by_id(@p_post_id, @p_viewer_id)",
            new { p_post_id = postId, p_viewer_id = viewerId });
    }

    public async Task<IEnumerable<UserPostSummary>> GetUserPostsAsync(Guid profileUserId, Guid? viewerId, int offset, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<UserPostSummary>(
            "SELECT * FROM sp_get_user_posts(@p_profile_user_id, @p_viewer_id, @p_offset, @p_limit)",
            new { p_profile_user_id = profileUserId, p_viewer_id = viewerId, p_offset = offset, p_limit = limit });
    }

    public async Task<PostCreated?> CreateAsync(Guid userId, string title, string? description, string? mediaUrl, string? thumbnailUrl, string mediaType, string[]? tags)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PostCreated>(
            "SELECT * FROM sp_create_post(@p_user_id, @p_title, @p_description, @p_media_url, @p_thumbnail_url, @p_media_type, @p_tags)",
            new { p_user_id = userId, p_title = title, p_description = description, p_media_url = mediaUrl, p_thumbnail_url = thumbnailUrl, p_media_type = mediaType, p_tags = tags ?? [] });
    }

    public async Task<OperationResult> DeleteAsync(Guid postId, Guid userId)
    {
        using var conn = db.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<OperationResult>(
            "SELECT success, message FROM sp_delete_post(@p_post_id, @p_user_id)",
            new { p_post_id = postId, p_user_id = userId });
        return result ?? new OperationResult { Success = false, Message = "Unknown error" };
    }

    public async Task<IEnumerable<PostSummary>> SearchAsync(string query, int offset, int limit)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<PostSummary>(
            "SELECT * FROM sp_search_posts(@p_query, @p_offset, @p_limit)",
            new { p_query = query, p_offset = offset, p_limit = limit });
    }
}