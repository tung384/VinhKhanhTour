using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using OneSProject.Models;
using OneSProject.Services;
using OneSProject.Services.Location;

namespace OneSProject
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _dbService;
        private readonly SyncService _syncService;
        private readonly LocationTracker _locationTracker;
        private LocationTracker? _tracker;

        public static LocationTracker? TrackerInstance { get; private set; }

        public MainPage()
        {
            InitializeComponent();

            // CHANGE: use shared services so sync and UI read the same local SQLite content.
            _dbService = App.GetService<DatabaseService>();
            _syncService = App.GetService<SyncService>();
            _locationTracker = App.GetService<LocationTracker>();
            CheckLocationPermissions();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await _syncService.InitializeAppContentAsync();
            await LoadPOIsToMap();

            if (_tracker == null && VinhKhanhMap.IsShowingUser)
            {
                _tracker = _locationTracker;
                TrackerInstance = _tracker;
                _ = _tracker.StartAsync();
            }
        }

        private async void CheckLocationPermissions()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                VinhKhanhMap.IsShowingUser = true;

                if (_tracker == null)
                {
                    _tracker = _locationTracker;
                    TrackerInstance = _tracker;
                    _ = _tracker.StartAsync();
                    System.Diagnostics.Debug.WriteLine("JARVIS: Location Tracker has been activated.");
                }
            }
        }

        private async Task LoadPOIsToMap()
        {
            var pois = await _dbService.GetAllPOIsAsync();
            VinhKhanhMap.Pins.Clear();

            foreach (var poi in pois)
            {
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = OneSProject.Resources.Languages.AppResources.TapToView,
                    Type = PinType.Place,
                    Location = new Location(poi.Latitude, poi.Longitude),
                    BindingContext = poi
                };

                pin.InfoWindowClicked += OnPinInfoWindowClicked;
                VinhKhanhMap.Pins.Add(pin);
            }

            if (pois.Count > 0)
            {
                var centerLocation = new Location(10.7605, 106.7045);
                var mapSpan = MapSpan.FromCenterAndRadius(centerLocation, Distance.FromMeters(300));
                VinhKhanhMap.MoveToRegion(mapSpan);
            }
        }

        private async void OnPinInfoWindowClicked(object? sender, PinClickedEventArgs e)
        {
            if (sender is Pin pin && pin.BindingContext is POI poi)
            {
                await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?SelectedPOIId={poi.Id}");
            }
        }
    }
}
