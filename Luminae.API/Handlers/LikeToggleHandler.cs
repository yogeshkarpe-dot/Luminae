using Luminae.API.Data.Repositories;
using Luminae.API.Models;
using System.Security.Claims;

namespace Luminae.API.Handlers;

public class LikeToggleHandler(ILikeRepository likes)
{
    public async Task<IResult> ToggleAsync(Guid postId, HttpContext ctx)
    {
        var userId = Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await likes.ToggleAsync(userId, postId);
        return result == null
            ? Results.NotFound()
            : Results.Ok(ApiResponse<LikeResult>.Ok(result));
    }
}