namespace VinhKhanhTour;

public partial class WebReviewPage : ContentPage
{
    public WebReviewPage()
    {
        InitializeComponent();
    }

    private void SetBindingToMainPage()
    {
        // JARVIS: Tìm kiếm MainPage chính xác hơn thông qua Application Stack
        MainPage? main = null;

        if (Application.Current?.MainPage is Shell shell)
        {
            // Tìm trong Shell
            main = shell.CurrentPage as MainPage;
        }
        else
        {
            main = Application.Current?.MainPage as MainPage;
        }

        if (main != null)
        {
            this.BindingContext = main;
            // JARVIS: Cực kỳ quan trọng - Thông báo cho UI rằng Restaurants đã sẵn sàng
            OnPropertyChanged(nameof(main.Restaurants));
            System.Diagnostics.Debug.WriteLine("JARVIS: Đã kết nối BindingContext thành công.");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SetBindingToMainPage();

        // JARVIS: Ép nạp lại nếu danh sách đang rỗng dù đã kết nối
        if (BindingContext is MainPage main && main.Restaurants.Count == 0)
        {
            _ = main.LoadDataFromSQLite();
        }
    }

    // Các hàm OnAddressTapped và OnSpeakClicked của Sir giữ nguyên 
    // (Vì chúng đã xử lý CommandParameter rất chuẩn)
    private async void OnAddressTapped(object sender, EventArgs e)
    {
        if (sender is Label lb && lb.GestureRecognizers[0] is TapGestureRecognizer tap && tap.CommandParameter is POI poi)
        {
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(poi.Location, new MapLaunchOptions { Name = poi.Name });
        }
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is POI poi && !string.IsNullOrWhiteSpace(poi.DescriptionVi))
        {
            await TextToSpeech.Default.SpeakAsync(poi.DescriptionVi);
        }
    }
}