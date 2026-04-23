using Microsoft.AspNetCore.Mvc;
using OneSBackend.DTOs;
using OneSBackend.Services;

namespace OneSBackend.Controllers;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly DeviceTrackingService _deviceTrackingService;

    public DeviceController(DeviceTrackingService deviceTrackingService)
    {
        _deviceTrackingService = deviceTrackingService;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] DeviceHeartbeatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required.");
        }

        await _deviceTrackingService.RegisterHeartbeatAsync(request, GetClientIpAddress());
        return Ok(new { message = "Heartbeat recorded." });
    }

    [HttpPost("poi-view")]
    public async Task<IActionResult> RecordPoiView([FromBody] DevicePoiViewRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId) || request.PoiId <= 0)
        {
            return BadRequest("DeviceId and PoiId are required.");
        }

        await _deviceTrackingService.RegisterPoiViewAsync(request);
        return Ok(new { message = "POI view recorded." });
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
