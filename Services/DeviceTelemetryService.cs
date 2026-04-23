using System.Diagnostics;
using Microsoft.Maui.Devices;

namespace OneSProject.Services;

public class DeviceTelemetryService
{
    private const string DeviceIdKey = "DeviceTelemetryId";
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(2);

    private readonly ApiService _apiService;
    private Task? _heartbeatLoopTask;
    private readonly object _heartbeatLock = new();

    public DeviceTelemetryService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public string GetDeviceId()
    {
        var deviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        deviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdKey, deviceId);
        return deviceId;
    }

    public async Task SendHeartbeatAsync()
    {
        try
        {
            await _apiService.SendHeartbeatAsync(new Models.DTOs.DeviceHeartbeatDto
            {
                DeviceId = GetDeviceId(),
                Platform = DeviceInfo.Platform.ToString(),
                DeviceName = DeviceInfo.Name,
                AppVersion = AppInfo.VersionString
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEVICE] Heartbeat failed: {ex.Message}");
        }
    }

    public void EnsureHeartbeatLoop()
    {
        lock (_heartbeatLock)
        {
            if (_heartbeatLoopTask != null)
            {
                return;
            }

            _heartbeatLoopTask = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(HeartbeatInterval);
                while (await timer.WaitForNextTickAsync())
                {
                    await SendHeartbeatAsync();
                }
            });
        }
    }

    public async Task RecordPoiViewAsync(int poiId)
    {
        try
        {
            await _apiService.RecordPoiViewAsync(new Models.DTOs.DevicePoiViewDto
            {
                DeviceId = GetDeviceId(),
                PoiId = poiId
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEVICE] POI view tracking failed: {ex.Message}");
        }
    }
}
