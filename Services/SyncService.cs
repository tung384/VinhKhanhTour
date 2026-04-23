using Microsoft.Maui.Networking;
using System.Diagnostics;

namespace OneSProject.Services;

public class SyncService
{
    private readonly ApiService _apiService;
    private readonly DatabaseService _dbService;
    private readonly DeviceTelemetryService _deviceTelemetryService;
    private static readonly string ImageCacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "synced-images");

    public SyncService(ApiService apiService, DatabaseService dbService, DeviceTelemetryService deviceTelemetryService)
    {
        _apiService = apiService;
        _dbService = dbService;
        _deviceTelemetryService = deviceTelemetryService;
    }

    public async Task InitializeAppContentAsync()
    {
        NetworkAccess accessType = Connectivity.Current.NetworkAccess;
        Debug.WriteLine($"[SYNC SYSTEM] Network access: {accessType}");

        if (accessType == NetworkAccess.Internet)
        {
            Debug.WriteLine("[SYNC SYSTEM] Internet available. Starting sync.");
            _deviceTelemetryService.EnsureHeartbeatLoop();
            await _deviceTelemetryService.SendHeartbeatAsync();
            await RunFullSyncAsync();
        }
        else
        {
            Debug.WriteLine("[SYNC SYSTEM] Offline. Using local content.");
            await _dbService.Init();
        }
    }

    private async Task RunFullSyncAsync()
    {
        try
        {
            int remoteVersion = await _apiService.GetVersionAsync();
            int localVersion = Preferences.Default.Get("ContentVersion", 0);
            bool localContentHealthy = await _dbService.IsContentHealthyAsync();
            Debug.WriteLine($"[SYNC] Evaluating sync. Remote={remoteVersion}, Local={localVersion}, Healthy={localContentHealthy}.");

            if (remoteVersion != localVersion || !localContentHealthy)
            {
                Debug.WriteLine($"[SYNC] Re-syncing content. Remote={remoteVersion}, Local={localVersion}, Healthy={localContentHealthy}.");

                var data = await _apiService.DownloadContentAsync();
                if (data?.POIs != null)
                {
                    await CacheImagesLocallyAsync(data);
                    await _dbService.ReplaceContentAsync(data);

                    var finalPois = await _dbService.GetAllPOIsAsync();
                    Debug.WriteLine("========================================");
                    Debug.WriteLine("[SYNC DIAGNOSTIC] Sync complete");
                    Debug.WriteLine($"[DATABASE] POIs count: {finalPois.Count}");
                    Debug.WriteLine($"[VERSION] System updated to: {data.Version}");
                    Debug.WriteLine("========================================");

                    Preferences.Default.Set("ContentVersion", data.Version);
                    Debug.WriteLine("[SYNC] Sync completed successfully.");
                }
                else
                {
                    Debug.WriteLine("[SYNC ERROR] Download returned no payload. Falling back to local content.");
                    await _dbService.Init();
                }
            }
            else
            {
                Debug.WriteLine("[SYNC] Local content is already current and healthy.");
                await _dbService.Init();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CRITICAL] Sync failed: {ex.Message}");
            await _dbService.Init();
        }
    }

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
