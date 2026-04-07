namespace OneSProject
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Ðang ký route cho trang chi tiết
            Routing.RegisterRoute(nameof(POIDetailPage), typeof(POIDetailPage));
        }
    }
}

