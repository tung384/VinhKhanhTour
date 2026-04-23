namespace OneSBackend.DTOs;

public class DeviceHeartbeatRequestDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
}

public class DevicePoiViewRequestDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int PoiId { get; set; }
}

public class DeviceSessionResponseDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsOnline { get; set; }
}

public class PoiViewStatDto
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public int TotalViews { get; set; }
    public int UniqueDevices { get; set; }
    public DateTime? LastViewedAt { get; set; }
}

public class ConnectedDevicesDashboardDto
{
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public List<DeviceSessionResponseDto> Devices { get; set; } = new();
    public List<PoiViewStatDto> TopPois { get; set; } = new();
}
