using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.DTOs;
using OneSBackend.Models;
using OneSBackend.Services;

namespace OneSBackend.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[Route("api/admin/accounts")]
public class AdminAccountsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ContentService _contentService;

    public AdminAccountsController(AppDbContext context, ContentService contentService)
    {
        _context = context;
        _contentService = contentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> GetAccounts()
    {
        var accounts = await _context.Accounts
            .Include(a => a.Poi)
            .OrderBy(a => a.Role)
            .ThenBy(a => a.Username)
            .Select(a => new AccountResponseDto
            {
                Id = a.Id,
                Username = a.Username,
                Password = a.Password,
                Role = a.Role,
                IsActive = a.IsActive,
                PoiId = a.PoiId,
                PoiName = a.Poi != null ? a.Poi.Name : null,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOwnerAccount([FromBody] OwnerAccountCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.StallName))
        {
            return BadRequest("Username, password, and stall name are required.");
        }

        var username = request.Username.Trim();
        if (await _context.Accounts.AnyAsync(a => a.Username == username))
        {
            return BadRequest("Username already exists.");
        }

        var qrCode = request.QrCode.Trim();
        if (string.IsNullOrWhiteSpace(qrCode))
        {
            return BadRequest("QR code is required.");
        }

        if (await _context.POIs.AnyAsync(p => p.QrCode == qrCode))
        {
            return BadRequest("QR code already exists.");
        }

        var poi = new POI
        {
            Name = request.StallName.Trim(),
            Latitude = 0,
            Longitude = 0,
            DetectionRadius = 25,
            Priority = 1,
            MainImage = string.Empty,
            QrCode = qrCode,
            IsActive = request.IsActive
        };

        _context.POIs.Add(poi);
        await _context.SaveChangesAsync();

        var account = new Account
        {
            Username = username,
            Password = request.Password,
            Role = AccountRoles.Owner,
            IsActive = true,
            PoiId = poi.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        return Ok(new
        {
            message = "Owner account created.",
            accountId = account.Id,
            poiId = poi.Id
        });
    }

    [HttpPost("backfill-existing-pois")]
    public async Task<IActionResult> BackfillExistingPois()
    {
        var unlinkedPois = await _context.POIs
            .Where(p => !_context.Accounts.Any(a => a.PoiId == p.Id))
            .OrderBy(p => p.Id)
            .ToListAsync();

        var createdAccounts = new List<object>();

        foreach (var poi in unlinkedPois)
        {
            var username = $"owner_poi_{poi.Id}";
            var password = $"VKPoi{poi.Id:000}";

            if (await _context.Accounts.AnyAsync(a => a.Username == username))
            {
                username = $"owner_poi_{poi.Id}_{Guid.NewGuid():N}".Substring(0, 20);
            }

            var account = new Account
            {
                Username = username,
                Password = password,
                Role = AccountRoles.Owner,
                IsActive = true,
                PoiId = poi.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            createdAccounts.Add(new
            {
                poiId = poi.Id,
                poiName = poi.Name,
                username,
                password
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            createdCount = createdAccounts.Count,
            accounts = createdAccounts
        });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] AccountStatusUpdateDto request)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        account.IsActive = request.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        if (account.PoiId.HasValue)
        {
            var poi = await _context.POIs.FirstOrDefaultAsync(p => p.Id == account.PoiId.Value);
            if (poi != null)
            {
                poi.IsActive = request.IsActive;
            }
        }

        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        return Ok(new { message = "Account and POI status updated." });
    }

    [HttpPut("{id}/password")]
    public async Task<IActionResult> UpdatePassword(int id, [FromBody] AccountPasswordUpdateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Password is required.");
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        account.Password = request.Password;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password updated." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account == null)
        {
            return NotFound();
        }

        if (account.Role == AccountRoles.Admin)
        {
            return BadRequest("Admin account cannot be deleted here.");
        }

        if (account.PoiId.HasValue)
        {
            var poi = await _context.POIs
                .Include(p => p.Images)
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == account.PoiId.Value);

            if (poi != null)
            {
                if (poi.Images?.Any() == true)
                {
                    _context.POIImages.RemoveRange(poi.Images);
                }

                if (poi.Translations?.Any() == true)
                {
                    _context.POITranslations.RemoveRange(poi.Translations);
                }

                _context.POIs.Remove(poi);
            }
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        return Ok(new { message = "Account deleted." });
    }
}
