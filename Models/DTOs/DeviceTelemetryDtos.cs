namespace OneSProject.Models.DTOs;

public class DeviceHeartbeatDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
}

public class DevicePoiViewDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int PoiId { get; set; }
}
