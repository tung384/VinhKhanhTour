using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject;

[QueryProperty(nameof(AutoPlay), "AutoPlay")]
[QueryProperty(nameof(POIId), "SelectedPOIId")]
public partial class POIDetailPage : ContentPage
{
    private readonly NarrationService _narrationService;
    private readonly DatabaseService _dbService;
    private POITranslation? _currentTranslation;
    public string AutoPlay { get; set; } = string.Empty;
    public int POIId { set => LoadData(value); }

    // Sử dụng Dependency Injection để nhận Singleton NarrationService
    public POIDetailPage(NarrationService narrationService)
    {
        InitializeComponent();
        _narrationService = narrationService;
        _dbService = new DatabaseService();
    }

    private async void LoadData(int id)
    {
        await _dbService.Init();

        // 1. Lấy thông tin quán và bản dịch
        var poi = await _dbService._database!.Table<POI>().Where(p => p.Id == id).FirstOrDefaultAsync();
        _currentTranslation = await _dbService.GetPOIWithTranslationAsync(id, "vi");

        if (poi != null) NameLabel.Text = poi.Name;

        if (_currentTranslation != null)
        {
            // Hiển thị mô tả dài lên giao diện
            DetailedDescriptionLabel.Text = _currentTranslation.DetailedDescription;
        }

        // 2. Nạp 3 ảnh từ bảng POIImage
        var images = await _dbService.GetPOIImagesAsync(id);
        ImageCarousel.ItemsSource = images;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (AutoPlay == "true")
        {
            // Tăng thời gian chờ lên một chút để đảm bảo LoadData hoàn tất
            await Task.Delay(2000);

            // Kiểm tra lại lần cuối trước khi kích hoạt âm thanh
            if (_currentTranslation != null)
            {
                OnAudioBtnClicked(this, EventArgs.Empty);
            }

            AutoPlay = "false";
        }
    }

    private async void OnAudioBtnClicked(object sender, EventArgs e)
    {
        if (_currentTranslation != null && !string.IsNullOrEmpty(_currentTranslation.DetailedDescription))
        {
            // Nâng cấp 3: Ép hệ thống đọc nội dung Mô tả chi tiết
            // Tạo một bản sao tạm thời để không ảnh hưởng đến dữ liệu gốc trong DB
            var tempTranslation = new POITranslation
            {
                LanguageCode = _currentTranslation.LanguageCode,
                AudioScript = _currentTranslation.DetailedDescription, // Gán mô tả chi tiết vào luồng đọc
                POIId = _currentTranslation.POIId
            };

            await _narrationService.PlayManualAsync(tempTranslation);
        }
    }
}