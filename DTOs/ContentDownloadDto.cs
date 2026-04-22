namespace OneSBackend.DTOs;

public class ContentDownloadDto
{
    public int Version { get; set; }
    public List<POIResponseDto> POIs { get; set; } = new();
}