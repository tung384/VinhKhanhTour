namespace VinhKhanhTour;

public partial class WebReviewPage : ContentPage
{
    public WebReviewPage() { InitializeComponent(); }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Application.Current?.MainPage is Shell shell && shell.CurrentPage is MainPage main)
            BindingContext = main;
    }

    private async void OnAddressTapped(object sender, EventArgs e)
    {
        if (sender is Label lb && lb.GestureRecognizers[0] is TapGestureRecognizer tap && tap.CommandParameter is POI poi)
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(poi.Location, new MapLaunchOptions { Name = poi.Name });
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is POI poi && !string.IsNullOrWhiteSpace(poi.DescriptionVi))
            await TextToSpeech.Default.SpeakAsync(poi.DescriptionVi);
    }
}