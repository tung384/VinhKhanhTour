using Microsoft.Extensions.Logging;
using OneSProject.Services;
using OneSProject.Services.Location;
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
            // CHANGE: register one shared service graph for the offline-first mobile app.
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<SyncService>();
            builder.Services.AddSingleton<NarrationService>();
            builder.Services.AddSingleton<LocationTracker>();
            builder.Services.AddTransient<QRScannerPage>();
            return builder.Build();
        }
    }
}

