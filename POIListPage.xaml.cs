using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject;

public partial class POIListPage : ContentPage
{
    private readonly DatabaseService _dbService;

    public POIListPage()
    {
        InitializeComponent();

        // CHANGE: list page now uses the shared local database service.
        _dbService = App.GetService<DatabaseService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        POICollection.ItemsSource = await _dbService.GetAllPOIsAsync();
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
