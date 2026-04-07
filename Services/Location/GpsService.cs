using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace OneSProject.Services.Location
{
    public class GpsService
    {
        public async Task<MauiLocation?> GetCurrentLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(3)
                );

                return await Geolocation.GetLocationAsync(request);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}