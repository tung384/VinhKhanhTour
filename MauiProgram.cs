using Microsoft.Extensions.Logging;
using OneSProject.Services;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;

namespace OneSProject
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiMaps()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<Services.DatabaseService>();
            builder.Services.AddSingleton<NarrationService>();
            builder.Services.AddTransient<POIDetailPage>();
            builder.Services.AddTransient<QRScannerPage>();
            return builder.Build();
        }
    }
}

