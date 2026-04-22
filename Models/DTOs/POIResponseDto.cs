namespace OneSProject.Models.DTOs;

public class POIResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double DetectionRadius { get; set; }
    public int Priority { get; set; }

    public string? MainImage { get; set; }
    public string? QrCode { get; set; }

    public List<TranslationDto> Translations { get; set; } = new();
    public List<string> Images { get; set; } = new();
}

public class TranslationDto
{
    public string LanguageCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DetailedDescription { get; set; }
    public string? AudioScript { get; set; }
    public string? AudioUrl { get; set; }
}