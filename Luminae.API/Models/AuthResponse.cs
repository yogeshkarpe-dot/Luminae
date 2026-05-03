namespace Luminae.API.Models;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public User User { get; set; } = new();
}

public class RegisterResult
{
    public Guid? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}