using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Newtonsoft.Json;
using System.Diagnostics;

namespace VinhKhanhTour;

public partial class MainPage : ContentPage
{
    // HttpClient dùng để gửi yêu cầu lấy dữ liệu từ API bên ngoài (OSRM)
    private readonly HttpClient _httpClient = new HttpClient();

    // Biến cờ để kiểm soát việc cập nhật GPS tự động khi đang hiển thị đường đi
    private bool _isManualRouting = false;

    public MainPage()
    {
        InitializeComponent();

        // Cấu hình bản đồ mặc định mở tại khu vực đường Vĩnh Khánh (Quận 4)
        vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(10.7600, 106.7050), Distance.FromKilometers(0.5)));

        // Chạy tiến trình theo dõi vị trí GPS ngầm
        _ = StartTracking();
    }

    // HÀM: Chu yển đổi giữa chế độ Tìm kiếm đơn giản và Chế độ chỉ đường
    private void OnToggleDirectionsModeClicked(object sender, EventArgs e)
    {
        // Nếu có nội dung ở ô tìm kiếm đơn, tự động chuyển sang ô Điểm đến của khung chỉ đường
        if (SingleSearchFrame.IsVisible && !string.IsNullOrWhiteSpace(SingleEndEntry.Text))
            EndEntry.Text = SingleEndEntry.Text;

        // Đảo trạng thái hiển thị của 2 khung giao diện (Ẩn cái này, hiện cái kia)
        SingleSearchFrame.IsVisible = !SingleSearchFrame.IsVisible;
        DirectionsFrame.IsVisible = !DirectionsFrame.IsVisible;

        // Tự động đưa con trỏ vào ô nhập liệu để người dùng gõ ngay
        if (DirectionsFrame.IsVisible)
        {
            if (string.IsNullOrWhiteSpace(StartEntry.Text)) StartEntry.Focus();
            else EndEntry.Focus();
        }
        else SingleEndEntry.Focus();
    }

    // HÀM XỬ LÝ CHÍNH: Tìm vị trí và vẽ đường đi khi nhấn nút
    private async void OnFindRouteClicked(object sender, EventArgs e)
    {
        try
        {
            // Xóa sạch các đường kẻ và ghim cũ trước khi tạo lộ trình mới
            vinhKhanhMap.MapElements.Clear();
            vinhKhanhMap.Pins.Clear();
            _isManualRouting = true; // Đánh dấu đang trong chế độ tìm đường

            Location startLocation = null;
            string startInput = StartEntry.Text;
            string endInput = DirectionsFrame.IsVisible ? EndEntry.Text : SingleEndEntry.Text;

            // Nếu không nhập điểm đến thì thoát hàm
            if (string.IsNullOrWhiteSpace(endInput)) return;

            // BƯỚC 1: Xác định điểm đi (Start)
            if (DirectionsFrame.IsVisible && !string.IsNullOrWhiteSpace(startInput))
            {
                // Chuyển tên địa chỉ người dùng gõ thành tọa độ (Geocoding)
                startLocation = (await Geocoding.GetLocationsAsync(startInput))?.FirstOrDefault();
                if (startLocation != null)
                    vinhKhanhMap.Pins.Add(new Pin { Label = "Điểm đi", Location = startLocation, Type = PinType.Place });
            }

            // Nếu người dùng không nhập điểm đi, mặc định lấy vị trí GPS hiện tại của máy
            if (startLocation == null)
                startLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

            // BƯỚC 2: Xác định điểm đến (Destination)
            var destination = (await Geocoding.GetLocationsAsync(endInput))?.FirstOrDefault();

            // BƯỚC 3: Nếu đủ cả 2 điểm thì tiến hành vẽ đường
            if (startLocation != null && destination != null)
            {
                // Cắm ghim cho điểm đến
                vinhKhanhMap.Pins.Add(new Pin { Label = endInput, Location = destination, Type = PinType.SearchResult });

                // Gọi hàm lấy dữ liệu đường đi từ Server OSRM
                await UpdateShortestRoute(startLocation, destination.Latitude, destination.Longitude);

                // Tính khoảng cách chim bay và tự động Zoom bản đồ cho vừa cả 2 điểm
                double dist = startLocation.CalculateDistance(destination, DistanceUnits.Kilometers);
                vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location((startLocation.Latitude + destination.Latitude) / 2,
                                 (startLocation.Longitude + destination.Longitude) / 2),
                    Distance.FromKilometers(dist * 1.5)));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tìm thấy địa chỉ hoặc mất kết nối mạng.", "OK");
        }
    }

    // HÀM: Lấy dữ liệu tọa độ đường đi từ OSRM API và vẽ Polyline
    public async Task UpdateShortestRoute(Location origin, double destLat, double destLng)
    {
        // URL API của OSRM (Sử dụng hệ tọa độ Longitude,Latitude)
        string url = $"https://router.project-osrm.org/route/v1/driving/{origin.Longitude},{origin.Latitude};{destLng},{destLat}?overview=full&geometries=polyline";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<OSRMResponse>(response);

            if (data?.Routes?.Count > 0)
            {
                // Giải mã chuỗi Polyline thành danh sách các điểm tọa độ
                var points = DecodePolyline(data.Routes[0].Geometry);

                // Cập nhật giao diện trên MainThread để tránh lỗi xung đột tiến trình
                MainThread.BeginInvokeOnMainThread(() => {
                    var polyline = new Microsoft.Maui.Controls.Maps.Polyline
                    {
                        StrokeColor = Color.FromArgb("#2196F3"), // Màu xanh dương
                        StrokeWidth = 10 // Độ dày đường kẻ
                    };
                    foreach (var p in points) polyline.Geopath.Add(p);
                    vinhKhanhMap.MapElements.Add(polyline); // Vẽ đường lên bản đồ

                    // Hiển thị tổng chiều dài quãng đường thực tế
                    statusLabel.Text = $"Quãng đường: {data.Routes[0].Distance / 1000:F2} km";
                });
            }
        }
        catch (Exception ex) { Debug.WriteLine("Lỗi API: " + ex.Message); }
    }

    // HÀM CHẠY NGẦM: Cập nhật GPS liên tục mỗi 5 giây
    private async Task StartTracking()
    {
        while (true)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5)));
                if (location != null && !_isManualRouting)
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        statusLabel.Text = $"Vị trí hiện tại: {location.Latitude:F4}, {location.Longitude:F4}";
                    });
                }
            }
            catch { }
            await Task.Delay(5000); // Nghỉ 5 giây rồi lặp lại
        }
    }

    // HÀM TOÁN HỌC: Giải mã chuỗi Polyline từ Google/OSRM sang tọa độ thực
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
}

// CÁC CLASS MODEL: Dùng để hứng dữ liệu trả về từ JSON API
public class OSRMResponse { [JsonProperty("routes")] public List<OSRMRoute> Routes { get; set; } }
public class OSRMRoute { [JsonProperty("geometry")] public string Geometry { get; set; } [JsonProperty("distance")] public double Distance { get; set; } }
//end of file