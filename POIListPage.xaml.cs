using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject;

public partial class POIListPage : ContentPage
{
    private DatabaseService _dbService = new DatabaseService();
    public POIListPage() { InitializeComponent(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _dbService.Init();
        POICollection.ItemsSource = await _dbService.GetAllPOIsAsync(); // Gọi hàm của Mem 3
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