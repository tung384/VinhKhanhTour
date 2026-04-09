using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject;

[QueryProperty(nameof(POIId), "SelectedPOIId")]
public partial class POIDetailPage : ContentPage
{
    private readonly NarrationService _narrationService;
    private readonly DatabaseService _dbService;
    private POITranslation? _currentTranslation;
    private int _id;

    public int POIId { set { _id = value; LoadData(value); } }

    public POIDetailPage(NarrationService narrationService)
    {
        InitializeComponent();
        _narrationService = narrationService;
        _dbService = new DatabaseService();
    }

    private async void LoadData(int id)
    {
        await _dbService.Init();

        // 1. Lấy ngôn ngữ từ Preferences (Phase 6.1)
        string selectedLang = Preferences.Default.Get("SelectedLanguage", "vi");

        // 2. Truy vấn đa ngôn ngữ (Đã có Fallback về 'vi' bên DatabaseService)
        _currentTranslation = await _dbService.GetPOIWithTranslationAsync(id, selectedLang);

        // 3. Lấy thông tin gốc (Tên quán)
        var poi = await _dbService._database!.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();

        if (poi != null)
        {
            NameLabel.Text = poi.Name;
            // Tự động lưu vào lịch sử (Phase 6.1 - Task 2)
            await _dbService.AddPOIToHistoryAsync(poi.Id);
        }

        if (_currentTranslation != null)
        {
            // Hiển thị kịch bản chi tiết lên Border
            DetailedDescriptionLabel.Text = _currentTranslation.DetailedDescription;
        }

        // 4. Nạp ảnh
        var images = await _dbService.GetPOIImagesAsync(id);
        ImageCarousel.ItemsSource = images;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Không cần truy vấn lại ở đây để tránh xung đột dữ liệu
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Gửi lệnh ngắt âm thanh ngay khi trang bị đóng hoặc ẩn đi
        _narrationService.Stop();
    }
    private async void OnAudioBtnClicked(object sender, EventArgs e)
    {
        if (_currentTranslation != null)
        {
            // Sử dụng mô tả chi tiết làm kịch bản nói
            await _narrationService.PlayManualAsync(_currentTranslation);
        }
    }
}