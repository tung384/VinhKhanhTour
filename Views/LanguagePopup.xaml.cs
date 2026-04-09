using CommunityToolkit.Maui.Views;
using System.Globalization;
using OneSProject.Resources.Languages;

namespace OneSProject.Views;

public partial class LanguagePopup : Popup
{
    public LanguagePopup()
    {
        InitializeComponent();
    }

    private async void OnLanguageSelected(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        string? lang = button.CommandParameter as string;

        if (!string.IsNullOrEmpty(lang))
        {
            // 1. Lưu cấu hình lựa chọn
            Preferences.Default.Set("SelectedLanguage", lang);
            Preferences.Default.Set("IsFirstRun", false);

            // 2. Cập nhật Culture ngay lập tức
            var culture = new CultureInfo(lang);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            AppResources.Culture = culture;

            // 3. ĐÓNG POPUP TRƯỚC (Để tránh lỗi PopupNotFoundException)
            await CloseAsync();

            // 4. RESET UI QUA WINDOW CHÍNH (Tiêu chuẩn .NET 9)
            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
    }
}