using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using OneSProject.Models.DTOs;

namespace OneSProject.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string DefaultAndroidEmulatorUrl = "https://10.0.2.2:7164/";
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
        return DefaultAndroidEmulatorUrl;
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] GetVersion failed: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] DownloadContent failed: {ex.Message}");
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
            Debug.WriteLine($"[API ERROR] DownloadBytes failed for {url}: {ex.Message}");
            return null;
        }
    }
}
