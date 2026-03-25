using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using SQLite;
using System.ComponentModel; // Thêm thư viện này
using System.Runtime.CompilerServices; // Thêm thư viện nà

namespace VinhKhanhTour;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient = new HttpClient();
    private SQLiteAsyncConnection? _database;
    private bool _isManualRouting = false;
    private ObservableCollection<POI> _restaurants = new();
    public ObservableCollection<POI> Restaurants
    {
        get => _restaurants;
        set
        {
            _restaurants = value;
            OnPropertyChanged(); // Thông báo khi gán danh sách mới
        }
    }

    public MainPage()
    {
        InitializeComponent();

        // Cấu hình bản đồ mặc định
        vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(10.7600, 106.7050), Distance.FromKilometers(0.5)));

        BindingContext = this;

        // Khởi động các hệ thống phụ trợ
        _ = InitDatabase();
        ////_ = StartTracking();
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    // --- QUẢN LÝ DATABASE ---
    private async Task InitDatabase()
    {
        try
        {
            SQLitePCL.Batteries_V2.Init();

            var dbName = "VinhKhanhData.db3";
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, dbName);

            if (!File.Exists(targetPath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(dbName);
                using var targetStream = File.Create(targetPath);
                await stream.CopyToAsync(targetStream);
            }

            _database = new SQLiteAsyncConnection(targetPath);
            await LoadDataFromSQLite();
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert("Hệ thống", $"Lỗi khởi tạo: {ex.Message}", "OK"));
        }
    }

    public async Task LoadDataFromSQLite()
    {
        try
        {
            if (_database == null) await InitDatabase();
            var items = await _database.Table<POI>().ToListAsync();

            Dispatcher.Dispatch(() =>
            {
                App.GlobalRestaurants.Clear();
                foreach (var item in items)
                {
                    App.GlobalRestaurants.Add(item);
                }
                // JARVIS: Cập nhật luôn cho chính nó nếu cần hiển thị trên Map
                this.Restaurants = App.GlobalRestaurants;
            });
        }
        catch (Exception ex) { /* Log error */ }
    }

    // --- XỬ LÝ ĐIỀU HƯỚNG & TÌM KIẾM ---
    private void OnToggleDirectionsModeClicked(object sender, EventArgs e)
    {
        if (DirectionsFrame != null && SingleSearchFrame != null)
        {
            DirectionsFrame.IsVisible = !DirectionsFrame.IsVisible;
            SingleSearchFrame.IsVisible = !SingleSearchFrame.IsVisible;
        }
    }

    private async void OnFindRouteClicked(object sender, EventArgs e)
    {
        if (_database == null) return;

        try
        {
            vinhKhanhMap.MapElements.Clear();
            vinhKhanhMap.Pins.Clear();
            _isManualRouting = true;

            string? startInput = StartEntry?.Text;
            string? endInput = DirectionsFrame.IsVisible ? EndEntry?.Text : SingleEndEntry?.Text;

            if (string.IsNullOrWhiteSpace(endInput)) return;

            // Tìm đích đến
            Location? destination = null;
            var localDestMatch = await _database.Table<POI>()
                .Where(x => x.Name.ToLower().Contains(endInput.ToLower()))
                .FirstOrDefaultAsync();

            destination = localDestMatch?.Location ?? (await Geocoding.GetLocationsAsync(endInput))?.FirstOrDefault();

            if (destination == null)
            {
                await DisplayAlert("Lỗi", "Không tìm thấy địa điểm này.", "OK");
                return;
            }

            // Xác định điểm đi
            Location? startLocation = null;
            if (!string.IsNullOrWhiteSpace(startInput))
            {
                var localStartMatch = await _database.Table<POI>()
                    .Where(x => x.Name.ToLower().Contains(startInput.ToLower()))
                    .FirstOrDefaultAsync();
                startLocation = localStartMatch?.Location ?? (await Geocoding.GetLocationsAsync(startInput))?.FirstOrDefault();
            }

            startLocation ??= await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));

            if (startLocation != null)
            {
                vinhKhanhMap.Pins.Add(new Pin { Label = "Điểm đi", Location = startLocation, Type = PinType.Generic });
                vinhKhanhMap.Pins.Add(new Pin { Label = "Đến: " + endInput, Location = destination, Type = PinType.Place });

                await UpdateShortestRoute(startLocation, destination.Latitude, destination.Longitude);
            }
        }
        catch (Exception ex)
        {
            // GPS bị tắt
            System.Diagnostics.Debug.WriteLine($"Lỗi StartTracking: {ex.Message}");
        }
    }

    // --- TRUY XUẤT OSRM & VẼ ĐƯỜNG ---
    public async Task UpdateShortestRoute(Location origin, double destLat, double destLng)
    {
        try
        {
            string url = $"https://router.project-osrm.org/route/v1/driving/{origin.Longitude},{origin.Latitude};{destLng},{destLat}?overview=full&geometries=polyline";
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<OSRMResponse>(response);

            if (data?.Routes != null && data.Routes.Count > 0)
            {
                var points = DecodePolyline(data.Routes[0].Geometry ?? "");
                var polyline = new Microsoft.Maui.Controls.Maps.Polyline
                {
                    StrokeColor = Color.FromArgb("#2196F3"),
                    StrokeWidth = 8
                };

                foreach (var p in points) polyline.Geopath.Add(p);
                vinhKhanhMap.MapElements.Add(polyline);

                if (statusLabel != null)
                    statusLabel.Text = $"Quãng đường: {data.Routes[0].Distance / 1000:F2} km";

                // Tự động căn chỉnh bản đồ
                var span = MapSpan.FromCenterAndRadius(
                    new Location((origin.Latitude + destLat) / 2, (origin.Longitude + destLng) / 2),
                    Distance.FromKilometers(1));
                vinhKhanhMap.MoveToRegion(span);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi OSRM: {ex.Message}");
        }
    }

    private List<Location> DecodePolyline(string encodedPoints)
    {
        var poly = new List<Location>();
        if (string.IsNullOrEmpty(encodedPoints)) return poly;

        int index = 0, lat = 0, lng = 0;
        while (index < encodedPoints.Length)
        {
            int b, shift = 0, result = 0;
            do { b = encodedPoints[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
            lat += ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
            shift = 0; result = 0;
            do { b = encodedPoints[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
            lng += ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
            poly.Add(new Location(lat / 1E5, lng / 1E5));
        }
        return poly;
    }

    private async Task StartTracking()
    {
        try
        {
            while (true)
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                if (location != null && !_isManualRouting && statusLabel != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        statusLabel.Text = $"Vị trí: {location.Latitude:F4}, {location.Longitude:F4}";
                    });
                }
                await Task.Delay(10000);
            }
        }
        catch (Exception ex)
        {
            // GPS bị tắt
            System.Diagnostics.Debug.WriteLine($"Lỗi StartTracking: {ex.Message}");
        }
    }
}

// --- MODELS ---
public class OSRMResponse { public List<OSRMRoute>? Routes { get; set; } }
public class OSRMRoute { public string? Geometry { get; set; } public double Distance { get; set; } }