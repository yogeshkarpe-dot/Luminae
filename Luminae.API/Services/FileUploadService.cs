namespace Luminae.API.Services;

public interface IFileUploadService
{
    Task<(string url, string? thumbnailUrl)> UploadAsync(IFormFile file, string subfolder);
    bool IsValidMediaFile(IFormFile file);
}

public class LocalFileUploadService(IConfiguration config, IWebHostEnvironment env) : IFileUploadService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".glb", ".gltf"];
    private readonly long _maxSize = long.Parse(config["Storage:MaxFileSizeBytes"] ?? "20971520");

    public bool IsValidMediaFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(ext) && file.Length <= _maxSize;
    }

    public async Task<(string url, string? thumbnailUrl)> UploadAsync(IFormFile file, string subfolder)
    {
        var uploadRoot = Path.Combine(env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(uploadRoot);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        var url = $"/uploads/{subfolder}/{fileName}";
        return (url, null);
    }
}