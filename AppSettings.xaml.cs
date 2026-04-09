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

    private async void OnRecentPOIsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("System", "Data loading...", "OK");
    }

    private async void OnAppInfoClicked(object sender, EventArgs e)
    {
        await DisplayAlert("OneSProject", "Version 1.0\nStark Industries Security Systems", "OK");
    }
}