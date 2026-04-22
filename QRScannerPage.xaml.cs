using OneSProject.Services;

namespace OneSProject;

public partial class QRScannerPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private bool _isNavigating = false;

    public QRScannerPage()
    {
        InitializeComponent();

        // CHANGE: scanner now queries the same shared local database as the rest of the app.
        _dbService = App.GetService<DatabaseService>();

        BarcodeReader.Options = new ZXing.Net.Maui.BarcodeReaderOptions
        {
            Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isNavigating = false;
        await _dbService.Init();
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
            await Shell.Current.GoToAsync("..");
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
