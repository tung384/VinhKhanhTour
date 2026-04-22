using Microsoft.Maui.Networking;
using System.Diagnostics;

namespace OneSProject.Services;

public class SyncService
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _dbService;
    private static readonly string ImageCacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "synced-images");

    public SyncService(ApiService apiService, DatabaseService dbService)
    {
        _apiService = apiService;
        _dbService = dbService;
    }

    public async Task InitializeAppContentAsync()
    {
        NetworkAccess accessType = Connectivity.Current.NetworkAccess;
        Debug.WriteLine($"[SYNC SYSTEM] Trạng thái mạng: {accessType}");

        if (accessType == NetworkAccess.Internet)
        {
            Debug.WriteLine("[SYNC SYSTEM] Có kết nối Internet. Khởi động quy trình Sync.");
            await RunFullSyncAsync();
        }
        else
        {
            Debug.WriteLine("[SYNC SYSTEM] Ngoại tuyến. Sử dụng dữ liệu lưu trữ nội bộ.");
            await _dbService.Init();
        }
    }

    private async Task RunFullSyncAsync()
    {
        try
        {
            int remoteVersion = await _apiService.GetVersionAsync();
            int localVersion = Preferences.Default.Get("ContentVersion", 0);

            if (remoteVersion > localVersion)
            {
                Debug.WriteLine($"[SYNC] Phát hiện phiên bản mới: {remoteVersion}. Đang tải dữ liệu...");

                var data = await _apiService.DownloadContentAsync();
                if (data?.POIs != null)
                {
                    await CacheImagesLocallyAsync(data);

                    // CHANGE: store the backend payload directly into SQLite for offline-first reads.
                    await _dbService.ReplaceContentAsync(data);
                    var finalPois = await _dbService.GetAllPOIsAsync();

                    Debug.WriteLine("========================================");
                    Debug.WriteLine("[JARVIS DIAGNOSTIC] Phase 5 Sync Complete");
                    Debug.WriteLine($"[DATABASE] POIs count: {finalPois.Count}");
                    Debug.WriteLine($"[VERSION] System updated to: {data.Version}");
                    Debug.WriteLine("========================================");

                    Preferences.Default.Set("ContentVersion", data.Version);
                    Debug.WriteLine("[SYNC] Đồng bộ hóa thành công.");
                }
            }
            else
            {
                Debug.WriteLine("[SYNC] Phiên bản hiện tại đã là mới nhất.");
                await _dbService.Init();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CRITICAL] Lỗi trong quá trình Sync: {ex.Message}");
            await _dbService.Init();
        }
    }

    // CHANGE: download synced POI images into local app storage and rewrite DTO paths for offline use.
    private async Task CacheImagesLocallyAsync(Models.DTOs.ContentDownloadDto content)
    {
        Directory.CreateDirectory(ImageCacheDirectory);

        foreach (var existingFile in Directory.GetFiles(ImageCacheDirectory))
        {
            File.Delete(existingFile);
        }

        foreach (var poi in content.POIs)
        {
            poi.MainImage = await CacheImageAsync(poi.MainImage) ?? poi.MainImage;

            var cachedImages = new List<string>();
            foreach (var imageUrl in poi.Images)
            {
                var cachedImage = await CacheImageAsync(imageUrl) ?? imageUrl;
                cachedImages.Add(cachedImage);
            }

            poi.Images = cachedImages;
        }
    }

    private async Task<string?> CacheImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return imageUrl;
        }

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
        {
            return imageUrl;
        }

        var extension = Path.GetExtension(imageUri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".img";
        }

        var localFileName = $"{Guid.NewGuid():N}{extension}";
        var localFilePath = Path.Combine(ImageCacheDirectory, localFileName);

        var imageBytes = await _apiService.DownloadBytesAsync(imageUrl);
        if (imageBytes == null)
        {
            return imageUrl;
        }

        await File.WriteAllBytesAsync(localFilePath, imageBytes);
        return localFilePath;
    }
}
