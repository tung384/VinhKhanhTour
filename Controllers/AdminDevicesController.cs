using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSBackend.Models;
using OneSBackend.Services;

namespace OneSBackend.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[Route("api/admin/devices")]
public class AdminDevicesController : ControllerBase
{
    private readonly DeviceTrackingService _deviceTrackingService;

    public AdminDevicesController(DeviceTrackingService deviceTrackingService)
    {
        _deviceTrackingService = deviceTrackingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _deviceTrackingService.GetDashboardAsync();
        return Ok(dashboard);
    }
}
