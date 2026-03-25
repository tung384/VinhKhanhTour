using System.Collections.ObjectModel;

namespace VinhKhanhTour
{
    public partial class App : Application
    {
        public static ObservableCollection<POI> GlobalRestaurants { get; set; } = new();
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        //protected override Window CreateWindow(IActivationState? activationState)
        //{
        //    return new Window(new AppShell());
        //}
    }
}