using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject;

[QueryProperty(nameof(POIId), "SelectedPOIId")]
[QueryProperty(nameof(AutoPlay), "AutoPlay")]
public partial class POIDetailPage : ContentPage
{
    private readonly NarrationService _narrationService;
    private readonly DatabaseService _dbService;
    private readonly DeviceTelemetryService _deviceTelemetryService;
    private POITranslation? _currentTranslation;
    private bool _shouldAutoPlay;

    public int POIId
    {
        set => LoadData(value);
    }

    public string? AutoPlay
    {
        set
        {
            _shouldAutoPlay = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            if (_shouldAutoPlay && _currentTranslation != null)
            {
                _ = _narrationService.PlayManualAsync(_currentTranslation);
            }
        }
    }

    public POIDetailPage()
    {
        InitializeComponent();
        _narrationService = App.GetService<NarrationService>();
        _dbService = App.GetService<DatabaseService>();
        _deviceTelemetryService = App.GetService<DeviceTelemetryService>();
    }

    private async void LoadData(int id)
    {
        string selectedLang = Preferences.Default.Get("SelectedLanguage", "vi");

        _currentTranslation = await _dbService.GetPOIWithTranslationAsync(id, selectedLang);
        var poi = await _dbService.GetPOIByIdAsync(id);

        if (poi != null)
        {
            NameLabel.Text = poi.Name;
            await _dbService.AddPOIToHistoryAsync(poi.Id);
            _ = _deviceTelemetryService.RecordPoiViewAsync(poi.Id);
        }

        if (_currentTranslation != null)
        {
            DetailedDescriptionLabel.Text = string.IsNullOrWhiteSpace(_currentTranslation.DetailedDescription)
                ? _currentTranslation.Description
                : _currentTranslation.DetailedDescription;

            if (_shouldAutoPlay)
            {
                await _narrationService.PlayManualAsync(_currentTranslation);
            }
        }
        else
        {
            DetailedDescriptionLabel.Text = string.Empty;
        }

        var images = await _dbService.GetPOIImagesAsync(id);
        ImageCarousel.ItemsSource = images;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MainPage.TrackerInstance?.Pause();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _narrationService.Stop();
        MainPage.TrackerInstance?.Resume();
    }

    private async void OnAudioBtnClicked(object sender, EventArgs e)
    {
        if (_currentTranslation != null)
        {
            await _narrationService.PlayManualAsync(_currentTranslation);
        }
    }
}
