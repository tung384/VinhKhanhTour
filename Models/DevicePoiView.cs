namespace OneSBackend.Models;

public class DevicePoiView
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public int PoiId { get; set; }
    public int ViewCount { get; set; }
    public DateTime FirstViewedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;

    public POI? Poi { get; set; }
}
