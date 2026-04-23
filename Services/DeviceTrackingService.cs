using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.DTOs;
using OneSBackend.Models;

namespace OneSBackend.Services;

public class DeviceTrackingService
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _context;

    public DeviceTrackingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegisterHeartbeatAsync(DeviceHeartbeatRequestDto request, string ipAddress)
    {
        var deviceId = request.DeviceId.Trim();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var session = await _context.DeviceSessions.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        if (session == null)
        {
            session = new DeviceSession
            {
                DeviceId = deviceId,
                Platform = request.Platform.Trim(),
                DeviceName = request.DeviceName.Trim(),
                AppVersion = request.AppVersion.Trim(),
                IpAddress = ipAddress,
                FirstSeenAt = now,
                LastSeenAt = now
            };

            _context.DeviceSessions.Add(session);
        }
        else
        {
            session.Platform = request.Platform.Trim();
            session.DeviceName = request.DeviceName.Trim();
            session.AppVersion = request.AppVersion.Trim();
            session.IpAddress = ipAddress;
            session.LastSeenAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task RegisterPoiViewAsync(DevicePoiViewRequestDto request)
    {
        var deviceId = request.DeviceId.Trim();
        if (string.IsNullOrWhiteSpace(deviceId) || request.PoiId <= 0)
        {
            return;
        }

        if (!await _context.POIs.AnyAsync(p => p.Id == request.PoiId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var existing = await _context.DevicePoiViews.FirstOrDefaultAsync(v => v.DeviceId == deviceId && v.PoiId == request.PoiId);
        if (existing == null)
        {
            _context.DevicePoiViews.Add(new DevicePoiView
            {
                DeviceId = deviceId,
                PoiId = request.PoiId,
                ViewCount = 1,
                FirstViewedAt = now,
                LastViewedAt = now
            });
        }
        else
        {
            existing.ViewCount += 1;
            existing.LastViewedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ConnectedDevicesDashboardDto> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var onlineCutoff = now - OnlineThreshold;

        var devices = await _context.DeviceSessions
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();

        var topPoiStats = await _context.DevicePoiViews
            .GroupBy(v => v.PoiId)
            .Select(group => new
            {
                PoiId = group.Key,
                TotalViews = group.Sum(x => x.ViewCount),
                UniqueDevices = group.Select(x => x.DeviceId).Distinct().Count(),
                LastViewedAt = group.Max(x => x.LastViewedAt)
            })
            .OrderByDescending(x => x.TotalViews)
            .ThenByDescending(x => x.LastViewedAt)
            .Take(10)
            .ToListAsync();

        var poiIds = topPoiStats.Select(x => x.PoiId).ToList();
        var poiNames = poiIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.POIs
                .Where(p => poiIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

        var topPois = topPoiStats
            .Select(x => new PoiViewStatDto
            {
                PoiId = x.PoiId,
                PoiName = poiNames.TryGetValue(x.PoiId, out var poiName) ? poiName : $"POI #{x.PoiId}",
                TotalViews = x.TotalViews,
                UniqueDevices = x.UniqueDevices,
                LastViewedAt = x.LastViewedAt
            })
            .ToList();

        return new ConnectedDevicesDashboardDto
        {
            TotalDevices = devices.Count,
            OnlineDevices = devices.Count(d => d.LastSeenAt >= onlineCutoff),
            Devices = devices.Select(d => new DeviceSessionResponseDto
            {
                DeviceId = d.DeviceId,
                Platform = d.Platform,
                DeviceName = d.DeviceName,
                AppVersion = d.AppVersion,
                IpAddress = d.IpAddress,
                FirstSeenAt = d.FirstSeenAt,
                LastSeenAt = d.LastSeenAt,
                IsOnline = d.LastSeenAt >= onlineCutoff
            }).ToList(),
            TopPois = topPois
        };
    }
}
