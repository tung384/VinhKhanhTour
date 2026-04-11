using OneSProject.Models;
using OneSProject.Services;

namespace OneSProject.Views;

public partial class RecentHistoryPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();

    public RecentHistoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Thuật toán lấy 5 POI gần nhất đã được tích hợp trong DatabaseService
        var history = await _dbService.GetRecentPOIsAsync();
        HistoryCollection.ItemsSource = history;
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI selected)
        {
            // Điều hướng về trang chi tiết kèm theo ID
            await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?SelectedPOIId={selected.Id}");
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}