using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject.Services.Location
{
    public class LocationTracker
    {
        private readonly GpsService _gps = new();
        private readonly GeofenceEngine _geo = new();
        private readonly DatabaseService _db = new();
        private readonly NarrationService _narrationService = new();
        private readonly string _language = "vi"; // tạm thời

        private bool _isRunning = false;

        public async Task StartAsync(List<POI> pois)
        {
            _isRunning = true;

            while (_isRunning)
            {
                var location = await _gps.GetCurrentLocationAsync();

                if (location != null)
                {
                    var (current, queue) = _geo.GetPrioritizedPOIs(location, pois);

                    if (current != null && _geo.ShouldTrigger(current, location))
                    {
                        var translations = new List<POITranslation>();

                        // 1. Current POI
                        var currentTranslation = await _db.GetTranslationAsync(current.Id, _language);
                        if (currentTranslation != null)
                            translations.Add(currentTranslation);

                        // 2. Queue POIs
                        foreach (var poi in queue)
                        {
                            var t = await _db.GetTranslationAsync(poi.Id, _language);
                            if (t != null)
                                translations.Add(t);
                        }

                        // 3. Gửi sang Audio System (Member 4)
                        await _narrationService.AddToQueueAsync(translations);
                    }
                }

                await Task.Delay(3000); // 3s
            }
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}