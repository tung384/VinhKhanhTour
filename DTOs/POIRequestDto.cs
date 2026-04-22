namespace OneSBackend.DTOs
{
    public class POIRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DetectionRadius { get; set; }
        public int Priority { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string MainImage { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public List<POIImageDto> Images { get; set; } = new();
        public List<POITranslationDto> Translations { get; set; } = new();
    }

    public class POIImageDto
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class POITranslationDto
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailedDescription { get; set; } = string.Empty;
        public string? AudioScript { get; set; }
        public string? AudioUrl { get; set; }
    }
}
