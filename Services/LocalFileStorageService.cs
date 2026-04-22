namespace OneSBackend.Services;

public class LocalFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // CHANGE: save uploaded POI images to local server storage instead of MySQL.
    public async Task<string> SavePoiImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.Combine(_environment.WebRootPath ?? GetDefaultWebRootPath(), "uploads", "poi");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName);
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, safeFileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/poi/{safeFileName}";
    }

    public void DeletePoiImageIfManaged(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/poi/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_environment.WebRootPath ?? GetDefaultWebRootPath(), relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string GetDefaultWebRootPath()
    {
        return Path.Combine(_environment.ContentRootPath, "wwwroot");
    }
}
