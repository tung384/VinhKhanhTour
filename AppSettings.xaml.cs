using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using OneSProject.Views;

namespace OneSProject;

public partial class AppSettings : ContentPage
{
    public AppSettings()
    {
        InitializeComponent();

        // Đặt trạng thái Switch dựa trên Preferences đã lưu
        DarkModeSwitch.IsToggled = Preferences.Default.Get("IsDarkMode", false);
        TtsSwitch.IsToggled = Preferences.Default.Get("IsTtsEnabled", true);
        VolumeSlider.Value = Preferences.Default.Get("TtsVolume", 1.0);
    }

    private async void OnLanguageClicked(object sender, EventArgs e)
    {
        var popup = new LanguagePopup();
        await this.ShowPopupAsync(popup);
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        // Cập nhật Theme hệ thống
        Application.Current!.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;

        // Lưu lại cấu hình
        Preferences.Default.Set("IsDarkMode", e.Value);
    }

    private void OnTtsToggled(object sender, ToggledEventArgs e)
    {
        // Lưu với tên khóa mới: IsTtsEnabled
        Preferences.Default.Set("IsTtsEnabled", e.Value);
    }

    private void OnVolumeChanged(object sender, ValueChangedEventArgs e)
    {
        Preferences.Default.Set("TtsVolume", e.NewValue);
    }

    private async void OnRecentPOIsClicked(object sender, EventArgs e)
    {
        // Chuyển hướng sang danh sách POI (Chúng ta sẽ lọc theo lịch sử sau)
        await Shell.Current.GoToAsync("RecentHistoryPage");
    }

    private async void OnAppInfoClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Vinh Khanh Multilingual App", "Version 1.0\nStark Industries", "OK");
    }
}