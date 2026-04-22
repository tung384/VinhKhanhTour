namespace OneSBackend.Models;

public class POIImage
{
    public int Id { get; set; }
    public int POIId { get; set; }

    public string? ImageUrl { get; set; }

    public POI? POI { get; set; }
}