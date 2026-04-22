namespace OneSBackend.Models;

public class POITranslation
{
    public int Id { get; set; }
    public int POIId { get; set; }

    public string LanguageCode { get; set; } = "vi";

    public string? Description { get; set; }
    public string? DetailedDescription { get; set; }

    public string? AudioScript { get; set; }
    public string? AudioUrl { get; set; }

    public POI? POI { get; set; }
}