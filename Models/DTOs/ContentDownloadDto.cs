namespace OneSProject.Models.DTOs;

public class ContentDownloadDto
{
    // CHANGE: align the mobile sync payload with the backend content contract.
    public int Version { get; set; }
    public List<POIResponseDto> POIs { get; set; } = new();
}
