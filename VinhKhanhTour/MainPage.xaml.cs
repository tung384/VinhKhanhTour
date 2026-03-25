using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace VinhKhanhTour;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient = new HttpClient();
    private bool _isManualRouting = false;
    public ObservableCollection<POI> Restaurants { get; set; } = new();

    public MainPage()
    {
        InitializeComponent();
        vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(10.7600, 106.7050), Distance.FromKilometers(0.5)));
        BindingContext = this;
        _ = StartTracking();
        //  Gọi hàm nạp từ SQL Server tại đây.
    }

    private void OnToggleDirectionsModeClicked(object sender, EventArgs e)
    {
        DirectionsFrame.IsVisible = !DirectionsFrame.IsVisible;
        SingleSearchFrame.IsVisible = !SingleSearchFrame.IsVisible;
    }

    private async void OnFindRouteClicked(object sender, EventArgs e)
    {
        try
        {
            vinhKhanhMap.MapElements.Clear();
            vinhKhanhMap.Pins.Clear();
            _isManualRouting = true;

            string endInput = DirectionsFrame.IsVisible ? EndEntry.Text : SingleEndEntry.Text;
            if (string.IsNullOrWhiteSpace(endInput)) return;

            var destination = (await Geocoding.GetLocationsAsync(endInput))?.FirstOrDefault();
            var startLocation = await Geolocation.GetLocationAsync();

            if (startLocation != null && destination != null)
            {
                vinhKhanhMap.Pins.Add(new Pin { Label = endInput, Location = destination });
                await UpdateShortestRoute(startLocation, destination.Latitude, destination.Longitude);
            }
        }
        catch { await DisplayAlert("Lỗi", "Không thể tìm thấy địa chỉ.", "OK"); }
    }

    public async Task UpdateShortestRoute(Location origin, double destLat, double destLng)
    {
        string url = $"https://router.project-osrm.org/route/v1/driving/{origin.Longitude},{origin.Latitude};{destLng},{destLat}?overview=full&geometries=polyline";
        var response = await _httpClient.GetStringAsync(url);
        var data = JsonConvert.DeserializeObject<OSRMResponse>(response);
        if (data?.Routes?.Count > 0)
        {
            var points = DecodePolyline(data.Routes[0].Geometry);
            var polyline = new Microsoft.Maui.Controls.Maps.Polyline { StrokeColor = Color.FromArgb("#2196F3"), StrokeWidth = 8 };
            foreach (var p in points) polyline.Geopath.Add(p);
            vinhKhanhMap.MapElements.Add(polyline);
            statusLabel.Text = $"Quãng đường: {data.Routes[0].Distance / 1000:F2} km";
        }
    }

    private List<Location> DecodePolyline(string encodedPoints)
    {
        var poly = new List<Location>();
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
        while (true)
        {
            var location = await Geolocation.Default.GetLocationAsync();
            if (location != null && !_isManualRouting)
                statusLabel.Text = $"Vị trí: {location.Latitude:F4}, {location.Longitude:F4}";
            await Task.Delay(5000);
        }
    }
}

// Models chuẩn hóa để nhận dữ liệu SQL Server
public class POI
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string DescriptionVi { get; set; }
    public Location Location { get; set; }
}

public class OSRMResponse { [JsonProperty("routes")] public List<OSRMRoute> Routes { get; set; } }
public class OSRMRoute { [JsonProperty("geometry")] public string Geometry { get; set; } [JsonProperty("distance")] public double Distance { get; set; } }