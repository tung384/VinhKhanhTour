using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject.Services.Location
{
    public class LocationTracker
    {
        private readonly GpsService _gps = new();
        private readonly DatabaseService _db;
        private readonly NarrationService _narrationService;
        private string _language => Preferences.Get("SelectedLanguage", "vi");
        private readonly HashSet<int> _currentZonePoiIds = new();
        private bool _isPaused = false;
        private bool _isRunning = false;

        public LocationTracker(DatabaseService db, NarrationService narrationService)
        {
            // CHANGE: tracker now owns enter/exit zone logic and nearest-first queueing.
            _db = db;
            _narrationService = narrationService;
        }

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

        public async Task StartAsync()
        {
            if (_isRunning)
            {
                return;
            }

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
                    var pois = await _db.GetAllPOIsAsync();
                    var inZone = pois
                        .Select(poi => new
                        {
                            Poi = poi,
                            Distance = DistanceCalculator.Calculate(
                                location.Latitude,
                                location.Longitude,
                                poi.Latitude,
                                poi.Longitude)
                        })
                        .Where(x => x.Distance <= x.Poi.DetectionRadius)
                        .OrderBy(x => x.Distance)
                        .Select(x => (x.Poi, x.Distance))
                        .ToList();

                    await QueueNewZoneEntriesAsync(inZone);

                    var latestZoneIds = inZone.Select(x => x.Poi.Id).ToHashSet();
                    _currentZonePoiIds.RemoveWhere(id => !latestZoneIds.Contains(id));
                }

                await Task.Delay(3000);
            }
        }

        // CHANGE: only newly entered POIs are queued, in nearest-to-farthest order.
        private async Task QueueNewZoneEntriesAsync(IEnumerable<(POI Poi, double Distance)> inZone)
        {
            var translations = new List<POITranslation>();

            foreach (var item in inZone)
            {
                var poi = item.Poi;

                if (_currentZonePoiIds.Contains(poi.Id))
                {
                    continue;
                }

                var translation = await _db.GetTranslationAsync(poi.Id, _language);
                if (translation != null)
                {
                    translations.Add(translation);
                    _currentZonePoiIds.Add(poi.Id);
                }
            }

            if (translations.Count > 0)
            {
                await _narrationService.AddToQueueAsync(translations);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _currentZonePoiIds.Clear();
        }
    }
}
