using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using OneSProject.Models.DTOs;

namespace OneSProject.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string DefaultAndroidLanHttpUrl = "http://192.168.247.209:5146/";
    private const string DefaultAndroidLanHttpsUrl = "https://192.168.247.209:7164/";
    private const string DefaultAndroidEmulatorUrl = "https://192.168.247.209:7164/";
    private const string DefaultDesktopUrl = "https://localhost:7164/";

    public ApiService()
    {
        // CHANGE: centralize the backend endpoint selection for the offline-first sync client.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(GetBaseUrl()),
            Timeout = TimeSpan.FromSeconds(20)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private static string GetBaseUrl()
    {
        var configured = Preferences.Default.Get("BackendBaseUrl", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.EndsWith("/") ? configured : $"{configured}/";
        }

#if ANDROID
        return DefaultAndroidLanHttpUrl;
#else
        return DefaultDesktopUrl;
#endif
    }

    public async Task<int> GetVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/content/version");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("version").GetInt32();
            }

            Debug.WriteLine($"[API ERROR] GetVersion returned status {(int)response.StatusCode} from {_httpClient.BaseAddress}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] GetVersion failed against {_httpClient.BaseAddress}: {ex.Message}");
        }

        return 0;
    }

    public async Task<ContentDownloadDto?> DownloadContentAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/content/download");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ContentDownloadDto>(json, _jsonOptions);
            }

            Debug.WriteLine($"[API ERROR] DownloadContent returned status {(int)response.StatusCode} from {_httpClient.BaseAddress}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] DownloadContent failed against {_httpClient.BaseAddress}: {ex.Message}");
        }

        return null;
    }

    // CHANGE: download remote image bytes during sync so the mobile app can stay offline-first.
    public async Task<byte[]?> DownloadBytesAsync(string url)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] DownloadBytes failed for {url} via {_httpClient.BaseAddress}: {ex.Message}");
            return null;
        }
    }

    public async Task SendHeartbeatAsync(DeviceHeartbeatDto dto)
    {
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/device/heartbeat", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RecordPoiViewAsync(DevicePoiViewDto dto)
    {
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/device/poi-view", content);
        response.EnsureSuccessStatusCode();
    }
}
