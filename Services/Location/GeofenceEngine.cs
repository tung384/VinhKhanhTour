using OneSProject.Models;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace OneSProject.Services.Location
{
    public class GeofenceEngine
    {
        private DateTime _lastTriggerTime = DateTime.MinValue;
        private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(10);

        public POI? GetNearestPOI(MauiLocation userLocation, List<POI> pois)
        {
            POI? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var poi in pois)
            {
                double distance = DistanceCalculator.Calculate(
                    userLocation.Latitude,
                    userLocation.Longitude,
                    poi.Latitude,
                    poi.Longitude
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = poi;
                }
            }

            return nearest;
        }

        public bool ShouldTrigger(POI poi, MauiLocation userLocation)
        {
            double distance = DistanceCalculator.Calculate(
                userLocation.Latitude,
                userLocation.Longitude,
                poi.Latitude,
                poi.Longitude
            );

            bool inside = distance <= poi.DetectionRadius;
            bool cooldownPassed = DateTime.Now - _lastTriggerTime > _cooldown;

            if (inside && cooldownPassed)
            {
                _lastTriggerTime = DateTime.Now;
                return true;
            }

            return false;
        }

        public (POI? current, List<POI> queue) GetPrioritizedPOIs(
            MauiLocation userLocation,
            List<POI> pois)
        {
            var sorted = pois
                .Select(poi => new
                {
                    Poi = poi,
                    Distance = DistanceCalculator.Calculate(
                        userLocation.Latitude,
                        userLocation.Longitude,
                        poi.Latitude,
                        poi.Longitude)
                })
                .OrderBy(x => x.Distance)
                .ToList();

            if (sorted.Count == 0)
                return (null, new List<POI>());

            var current = sorted.First().Poi;
            var queue = sorted.Skip(1).Select(x => x.Poi).ToList();

            return (current, queue);
        }
    }
}