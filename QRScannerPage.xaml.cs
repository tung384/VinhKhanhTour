using OneSProject.Services;

namespace OneSProject;

public partial class QRScannerPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private bool _isNavigating = false; // Ngăn chặn việc quét trùng lặp khi đang điều hướng

    public QRScannerPage(DatabaseService dbService)
    {
        InitializeComponent();
        _dbService = new DatabaseService();

        BarcodeReader.Options = new ZXing.Net.Maui.BarcodeReaderOptions
        {
            Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false // Chỉ tập trung vào 1 mã duy nhất
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckCameraPermissions();
    }

    private async Task CheckCameraPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Cảnh báo", "Ứng dụng cần quyền Camera để quét mã QR.", "OK");
            await Shell.Current.GoToAsync(".."); // Quay lại trang trước nếu không cấp quyền
        }
    }

    private async void OnBarcodesDetected(object sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if (_isNavigating) return;

        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        _isNavigating = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var poi = await _dbService.GetPOIByQrCodeAsync(result.Value);

            if (poi != null)
            {
                // Truyền thêm tham số AutoPlay=true để trang chi tiết tự động đọc thuyết minh
                await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?SelectedPOIId={poi.Id}&AutoPlay=true");
            }
            else
            {
                await DisplayAlert("Thông báo", "Mã QR không hợp lệ hoặc quán chưa đăng ký.", "OK");
                _isNavigating = false;
            }
        });
    }
}