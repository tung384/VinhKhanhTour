using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui; // Thêm Toolkit để hỗ trợ UI tốt hơn
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace VinhKhanhTour;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // Kích hoạt Toolkit
            .UseMauiMaps() // BẮT BUỘC: Để bản đồ hiển thị được
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Đăng ký các Page dưới dạng Singleton để tiết kiệm bộ nhớ khi dùng SQL
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<WebReviewPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}