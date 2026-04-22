using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject.Views;

public partial class RecentHistoryPage : ContentPage
{
    private readonly DatabaseService _dbService;

    public RecentHistoryPage()
    {
        InitializeComponent();

        // CHANGE: history page now uses the shared local database service.
        _dbService = App.GetService<DatabaseService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var history = await _dbService.GetRecentPOIsAsync();
        HistoryCollection.ItemsSource = history;
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI selected)
        {
            await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?SelectedPOIId={selected.Id}");
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
