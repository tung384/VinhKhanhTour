using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
// Thêm namespace Models và Services của ngài ở đây
using OneSProject.Models;
using OneSProject.Services;
using OneSProject.Services.Location;

namespace OneSProject
{
    public partial class MainPage : ContentPage
    {
        private DatabaseService _dbService;
        private LocationTracker? _tracker;
        public MainPage()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            CheckLocationPermissions();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Khởi tạo DB và nạp dữ liệu (Kết nối với Giai đoạn 2 của Thành viên 3)
            await _dbService.Init();

            // Tải các điểm lên bản đồ
            await LoadPOIsToMap();

            var pois = await _dbService.GetAllPOIsAsync();

            // Chỉ start nếu đã có permission và chưa chạy
            if (_tracker == null && VinhKhanhMap.IsShowingUser)
            {
                _tracker = new LocationTracker();
                _ = _tracker.StartAsync(pois);
            }
        }

        private async void CheckLocationPermissions()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                // Khi có quyền, bản đồ sẽ tự động tìm vị trí người dùng
                VinhKhanhMap.IsShowingUser = true;

                if (_tracker == null)
                {
                    var pois = await _dbService.GetAllPOIsAsync();

                    _tracker = new LocationTracker();
                    _ = _tracker.StartAsync(pois);
                }
            }
        }
        private async Task LoadPOIsToMap()
        {
            // 1. Lấy danh sách POI từ Database (Cần đảm bảo Thành viên 3 có hàm GetPOIsAsync, 
            // hoặc viết trực tiếp truy vấn SQLite ở đây)
            var pois = await _dbService._database!.Table<POI>().ToListAsync();

            // Xóa các điểm cũ nếu có
            VinhKhanhMap.Pins.Clear();

            // 2. Tạo Pins và gắn lên bản đồ
            foreach (var poi in pois)
            {
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = "Nhấn để xem chi tiết quầy", // Chú thích để người dùng biết
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude)
                };

                // 3. Gắn sự kiện khi người dùng click vào bong bóng thông tin
                pin.InfoWindowClicked += OnPinInfoWindowClicked;

                VinhKhanhMap.Pins.Add(pin);
            }

            // 4. Focus Camera về khu vực Vĩnh Khánh (Lấy tọa độ trung tâm mô phỏng)
            if (pois.Count > 0)
            {
                // Tọa độ trung tâm Vĩnh Khánh (khoảng giữa Ốc Oanh và Lẩu Bò)
                var centerLocation = new Location(10.7605, 106.7045);
                // Bán kính hiển thị 300 mét
                var mapSpan = MapSpan.FromCenterAndRadius(centerLocation, Distance.FromMeters(300));

                VinhKhanhMap.MoveToRegion(mapSpan);
            }
        }

        // Sự kiện chuyển hướng sang Trang Chi tiết (Giai đoạn 6)
        private async void OnPinInfoWindowClicked(object? sender, PinClickedEventArgs e)
        {
            // Pin được nhấn sẽ chứa đối tượng POI trong BindingContext
            if (sender is Pin pin && pin.BindingContext is POI poi)
            {
                // Chú ý: Phải dùng "SelectedPOIId" để khớp với POIDetailPage
                await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?SelectedPOIId={poi.Id}");
            }
        }
    }
}