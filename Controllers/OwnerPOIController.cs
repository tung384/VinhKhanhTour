using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.DTOs;
using OneSBackend.Models;
using OneSBackend.Services;

namespace OneSBackend.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Owner)]
[Route("api/owner/poi")]
public class OwnerPOIController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ContentService _contentService;
    private readonly LocalFileStorageService _fileStorageService;

    public OwnerPOIController(AppDbContext context, ContentService contentService, LocalFileStorageService fileStorageService)
    {
        _context = context;
        _contentService = contentService;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyPoi()
    {
        var accountResult = await GetCurrentAccountResultAsync();
        if (accountResult.ErrorResult != null)
        {
            return accountResult.ErrorResult;
        }

        var account = accountResult.Account;
        if (account == null || !account.PoiId.HasValue)
        {
            return NotFound();
        }

        var poi = await _context.POIs
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == account.PoiId.Value);

        if (poi == null)
        {
            return NotFound();
        }

        return Ok(new POIResponseDto
        {
            Id = poi.Id,
            Name = poi.Name,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            DetectionRadius = poi.DetectionRadius,
            Priority = poi.Priority,
            MainImage = ToAbsoluteUrl(poi.MainImage),
            QrCode = poi.QrCode,
            IsActive = poi.IsActive,
            Translations = poi.Translations?.Select(t => new TranslationDto
            {
                LanguageCode = t.LanguageCode,
                Description = t.Description,
                DetailedDescription = t.DetailedDescription,
                AudioScript = t.AudioScript,
                AudioUrl = t.AudioUrl
            }).ToList() ?? new List<TranslationDto>(),
            Images = BuildAbsoluteImageUrls(poi.MainImage, poi.Images?.Select(i => i.ImageUrl)).ToList()
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMyPoi([FromBody] OwnerPoiUpdateDto dto)
    {
        var accountResult = await GetCurrentAccountResultAsync();
        if (accountResult.ErrorResult != null)
        {
            return accountResult.ErrorResult;
        }

        var account = accountResult.Account;
        if (account == null || !account.PoiId.HasValue)
        {
            return NotFound();
        }

        var poi = await _context.POIs
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == account.PoiId.Value);

        if (poi == null)
        {
            return NotFound();
        }

        var validationError = ValidatePoi(dto);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        poi.Name = dto.Name;
        poi.Latitude = dto.Latitude;
        poi.Longitude = dto.Longitude;
        poi.DetectionRadius = dto.DetectionRadius;
        poi.MainImage = NormalizeStoredImageUrl(dto.MainImage);

        if (poi.Images?.Any() == true)
        {
            _context.POIImages.RemoveRange(poi.Images);
        }

        if (poi.Translations?.Any() == true)
        {
            _context.POITranslations.RemoveRange(poi.Translations);
        }

        foreach (var imageUrl in BuildNormalizedImageUrls(dto.MainImage, dto.Images.Select(x => x.ImageUrl)))
        {
            _context.POIImages.Add(new POIImage
            {
                POIId = poi.Id,
                ImageUrl = imageUrl
            });
        }

        foreach (var tr in dto.Translations)
        {
            _context.POITranslations.Add(new POITranslation
            {
                POIId = poi.Id,
                LanguageCode = tr.LanguageCode,
                Description = tr.Description,
                DetailedDescription = tr.DetailedDescription,
                AudioScript = tr.AudioScript,
                AudioUrl = tr.AudioUrl
            });
        }

        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        return Ok(new { message = "POI updated." });
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequestDto request, CancellationToken cancellationToken)
    {
        var accountResult = await GetCurrentAccountResultAsync();
        if (accountResult.ErrorResult != null)
        {
            return accountResult.ErrorResult;
        }

        if (accountResult.Account == null)
        {
            return Unauthorized();
        }

        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest("Image file is required.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Only jpg, jpeg, png, and webp files are supported.");
        }

        var imageUrl = await _fileStorageService.SavePoiImageAsync(file, cancellationToken);
        return Ok(new { imageUrl });
    }

    private async Task<(Account? Account, IActionResult? ErrorResult)> GetCurrentAccountResultAsync()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var accountId))
        {
            return (null, Unauthorized());
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        if (account == null)
        {
            return (null, Unauthorized());
        }

        if (!account.IsActive)
        {
            return (null, StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "ACCOUNT_INACTIVE",
                message = "You must pay before continue access the page."
            }));
        }

        return (account, null);
    }

    private static string? ValidatePoi(OwnerPoiUpdateDto dto)
    {
        if (dto.Latitude < -90 || dto.Latitude > 90)
        {
            return "Latitude must be between -90 and 90.";
        }

        if (dto.Longitude < -180 || dto.Longitude > 180)
        {
            return "Longitude must be between -180 and 180.";
        }

        if (!dto.Translations.Any(t => t.LanguageCode == "vi"))
        {
            return "Vietnamese (vi) translation is required.";
        }

        if (dto.Translations.GroupBy(t => t.LanguageCode).Any(g => g.Count() > 1))
        {
            return "Duplicate languageCode detected.";
        }

        if (BuildNormalizedImageUrls(dto.MainImage, dto.Images.Select(x => x.ImageUrl)).Count() > 10)
        {
            return "A POI can have at most 10 images.";
        }

        return null;
    }

    private IEnumerable<string> BuildAbsoluteImageUrls(string? mainImage, IEnumerable<string?>? imageUrls)
    {
        foreach (var imageUrl in BuildNormalizedImageUrls(mainImage, imageUrls))
        {
            var absoluteUrl = ToAbsoluteUrl(imageUrl);
            if (!string.IsNullOrWhiteSpace(absoluteUrl))
            {
                yield return absoluteUrl;
            }
        }
    }

    private static IEnumerable<string> BuildNormalizedImageUrls(string? mainImage, IEnumerable<string?>? imageUrls)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var orderedUrls = new List<string?>();

        if (!string.IsNullOrWhiteSpace(mainImage))
        {
            orderedUrls.Add(mainImage);
        }

        if (imageUrls != null)
        {
            orderedUrls.AddRange(imageUrls);
        }

        foreach (var candidate in orderedUrls)
        {
            var normalized = NormalizeStoredImageUrl(candidate);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                continue;
            }

            yield return normalized;
        }
    }

    private static string NormalizeStoredImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return uri.PathAndQuery;
        }

        return imageUrl;
    }

    private string? ToAbsoluteUrl(string? maybeRelativeUrl)
    {
        if (string.IsNullOrWhiteSpace(maybeRelativeUrl))
        {
            return maybeRelativeUrl;
        }

        if (Uri.TryCreate(maybeRelativeUrl, UriKind.Absolute, out _))
        {
            return maybeRelativeUrl;
        }

        return $"{Request.Scheme}://{Request.Host}{maybeRelativeUrl}";
    }
}
