namespace OneSBackend.DTOs;

public class OwnerPoiUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DetectionRadius { get; set; }
    public string MainImage { get; set; } = string.Empty;
    public List<POIImageDto> Images { get; set; } = new();
    public List<POITranslationDto> Translations { get; set; } = new();
}
