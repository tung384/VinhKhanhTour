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
        public static LocationTracker? TrackerInstance { get; private set; }
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
                TrackerInstance = _tracker;
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
                VinhKhanhMap.IsShowingUser = true;
                // Đảm bảo Tracker khởi chạy ngay lập tức sau khi có quyền
                if (_tracker == null)
                {
                    await _dbService.Init();
                    var pois = await _dbService.GetAllPOIsAsync();
                    _tracker = new LocationTracker();
                    _ = _tracker.StartAsync(pois);
                    System.Diagnostics.Debug.WriteLine("JARVIS: Location Tracker has been activated.");
                }
            }
        }
        private async Task LoadPOIsToMap()
        {
            var pois = await _dbService._database!.Table<POI>().ToListAsync();
            VinhKhanhMap.Pins.Clear();

            // Lấy ngôn ngữ hiện tại từ Preferences
            string lang = Preferences.Get("SelectedLanguage", "vi");

            foreach (var poi in pois)
            {
                // Truy vấn bản dịch cho từng điểm để lấy tên và địa chỉ đã dịch
                var translation = await _dbService.GetPOIWithTranslationAsync(poi.Id, lang);

                var pin = new Pin
                {
                    // Nếu có bản dịch thì dùng Name trong bản dịch (nếu ngài có trường Name trong POITranslation)
                    // Hoặc đơn giản là dùng chuỗi hướng dẫn từ AppResources
                    Label = poi.Name,
                    Address = OneSProject.Resources.Languages.AppResources.TapToView,
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude),
                    BindingContext = poi
                };

                pin.InfoWindowClicked += OnPinInfoWindowClicked;
                VinhKhanhMap.Pins.Add(pin);

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