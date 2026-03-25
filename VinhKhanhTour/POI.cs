using SQLite;

namespace VinhKhanhTour;

public class POI
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string DescriptionVi { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = "dotnet_bot.svg";

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [Ignore]
    public Location Location => new Location(Latitude, Longitude);
}