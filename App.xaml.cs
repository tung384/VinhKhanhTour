using CommunityToolkit.Maui.Extensions;
using OneSProject.Resources.Languages;
using OneSProject.Services;
using System.Globalization;

namespace OneSProject
{
    public partial class App : Application
    {
        public static DatabaseService DatabaseService { get; set; } = new DatabaseService();
        public App()
        {
            InitializeComponent();
            // 1.Đồng bộ ngôn ngữ ngay từ khi khởi động
            string lang = Preferences.Default.Get("SelectedLanguage", "vi");
            var culture = new CultureInfo(lang);
            AppResources.Culture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // 2. Đồng bộ Chế độ tối
            bool isDarkMode = Preferences.Default.Get("IsDarkMode", false);
            Application.Current!.UserAppTheme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            // 2. Kiểm tra lần đầu chạy sau khi Window đã sẵn sàng
            CheckFirstRun(window);
            return window;
        }

        //private async void CheckFirstRun(Window window)
        //{
        //    bool isFirstRun = Preferences.Default.Get("IsFirstRun", true);
        //    if (isFirstRun)
        //    {
        //        await Task.Delay(1000);
        //        var popup = new Views.LanguagePopup();
        //        // Sử dụng window.Page để tránh lỗi Shell.Current null
        //        await MainThread.InvokeOnMainThreadAsync(() => window.Page!.ShowPopup(popup));
        //        if (result is string lang)
        //        {
        //            Preferences.Default.Set("SelectedLanguage", lang);

        //            var culture = new CultureInfo(lang);
        //            AppResources.Culture = culture;
        //            CultureInfo.DefaultThreadCurrentCulture = culture;
        //            CultureInfo.DefaultThreadCurrentUICulture = culture;
        //        }

        //        Preferences.Default.Set("IsFirstRun", false);
        //    }
        //}
        private async void CheckFirstRun(Window window)
        {
            bool isFirstRun = Preferences.Default.Get("IsFirstRun", true);

            if (isFirstRun)
            {
                await Task.Delay(1000);

                var popup = new Views.LanguagePopup();

                object result = await window.Page!.ShowPopupAsync(popup);

                if (result is string lang)
                {
                    Preferences.Default.Set("SelectedLanguage", lang);

                    var culture = new CultureInfo(lang);
                    AppResources.Culture = culture;
                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                }

                Preferences.Default.Set("IsFirstRun", false);
            }
        }
    }
}
