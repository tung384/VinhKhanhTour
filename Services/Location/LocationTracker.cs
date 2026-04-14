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
        private string _language => Preferences.Get("SelectedLanguage", "vi");
        private readonly HashSet<int> _playedPoiIds = new();
        private const int MAX_POI = 2;
        private bool _isPaused = false;

        private bool _isRunning = false;

        public void Pause()
        {
            _isPaused = true;
            System.Diagnostics.Debug.WriteLine("JARVIS: Tracker Paused");
        }

        public void Resume()
        {
            _isPaused = false;
            System.Diagnostics.Debug.WriteLine("JARVIS: Tracker Resumed");
        }

        public async Task StartAsync(List<POI> pois)
        {
            await _db.Init();
            _isRunning = true;

            while (_isRunning)
            {
                if (_isPaused)
                {
                    await Task.Delay(1000);
                    continue;
                }

                var location = await _gps.GetCurrentLocationAsync();

                if (location != null)
                {
                    var prioritized = pois
                    .Select(poi => new
                    {
                        Poi = poi,
                        Distance = DistanceCalculator.Calculate(
                            location.Latitude,
                            location.Longitude,
                            poi.Latitude,
                            poi.Longitude)
                    })
                    .Where(x => x.Distance <= x.Poi.DetectionRadius) // ✅ trong phạm vi
                    .OrderBy(x => x.Distance)
                    .Take(MAX_POI) // ✅ chỉ lấy 1-2 POI
                    .Select(x => x.Poi)
                    .ToList();

                    if (prioritized.Count > 0)
                    {
                        var translations = new List<POITranslation>();

                        foreach (var poi in prioritized)
                        {
                            // ✅ bỏ qua nếu đã phát
                            if (_playedPoiIds.Contains(poi.Id))
                                continue;

                            var translation = await _db.GetTranslationAsync(poi.Id, _language);

                            if (translation != null)
                            {
                                translations.Add(translation);
                                _playedPoiIds.Add(poi.Id); // đánh dấu đã phát
                            }
                        }

                        // ✅ chỉ gọi khi có POI mới
                        if (translations.Count > 0)
                        {
                            await _narrationService.AddToQueueAsync(translations);
                        }
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