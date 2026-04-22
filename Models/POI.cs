namespace OneSBackend.Models;

public class POI
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double DetectionRadius { get; set; }
    public int Priority { get; set; }

    public string? MainImage { get; set; }
    public string? QrCode { get; set; }

    public bool IsActive { get; set; }

    public List<POITranslation>? Translations { get; set; }
    public List<POIImage>? Images { get; set; }
}