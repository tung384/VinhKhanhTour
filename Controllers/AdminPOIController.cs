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
[Route("api/admin/poi")]
public class AdminPOIController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ContentService _contentService;
    private readonly LocalFileStorageService _fileStorageService;

    public AdminPOIController(
        AppDbContext context,
        ContentService contentService,
        LocalFileStorageService fileStorageService)
    {
        _context = context;
        _contentService = contentService;
        _fileStorageService = fileStorageService;
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequestDto request, CancellationToken cancellationToken)
    {
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

    [HttpPost]
    public async Task<IActionResult> CreatePOI([FromBody] POIRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var validationError = ValidatePOI(dto);
        if (validationError != null)
            return BadRequest(validationError);

        var poi = new POI
        {
            Name = dto.Name,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            DetectionRadius = dto.DetectionRadius,
            Priority = dto.Priority,
            QrCode = dto.QrCode,
            MainImage = NormalizeStoredImageUrl(dto.MainImage),
            IsActive = dto.IsActive
        };

        _context.POIs.Add(poi);
        await _context.SaveChangesAsync();

        AddChildCollections(poi.Id, dto);
        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        return Ok(new { message = "POI created", id = poi.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePOI(int id, [FromBody] POIRequestDto dto)
    {
        var poi = await _context.POIs
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi == null)
            return NotFound();

        var validationError = ValidatePOI(dto);
        if (validationError != null)
            return BadRequest(validationError);

        var oldImageUrls = BuildNormalizedImageUrls(poi.MainImage, poi.Images?.Select(x => x.ImageUrl)).ToList();

        poi.Name = dto.Name;
        poi.Latitude = dto.Latitude;
        poi.Longitude = dto.Longitude;
        poi.DetectionRadius = dto.DetectionRadius;
        poi.Priority = dto.Priority;
        poi.QrCode = dto.QrCode;
        poi.MainImage = NormalizeStoredImageUrl(dto.MainImage);
        poi.IsActive = dto.IsActive;

        if (poi.Images?.Any() == true)
        {
            _context.POIImages.RemoveRange(poi.Images);
        }

        if (poi.Translations?.Any() == true)
        {
            _context.POITranslations.RemoveRange(poi.Translations);
        }

        AddChildCollections(id, dto);
        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        var activeImageUrls = BuildNormalizedImageUrls(dto.MainImage, dto.Images.Select(x => x.ImageUrl))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var oldImageUrl in oldImageUrls.Where(url => !activeImageUrls.Contains(url)))
        {
            _fileStorageService.DeletePoiImageIfManaged(oldImageUrl);
        }

        return Ok(new { message = "POI updated" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePOI(int id)
    {
        var poi = await _context.POIs
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi == null)
            return NotFound();

        var imageUrls = poi.Images?.Select(x => x.ImageUrl).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList() ?? new List<string>();

        if (poi.Images?.Any() == true)
        {
            _context.POIImages.RemoveRange(poi.Images);
        }

        if (poi.Translations?.Any() == true)
        {
            _context.POITranslations.RemoveRange(poi.Translations);
        }

        _context.POIs.Remove(poi);
        await _context.SaveChangesAsync();
        await _contentService.IncreaseVersionAsync();

        foreach (var imageUrl in imageUrls)
        {
            _fileStorageService.DeletePoiImageIfManaged(imageUrl);
        }

        return Ok(new { message = "POI deleted" });
    }

    // CHANGE: keep POI child records aligned with the mobile app's SQLite schema.
    private void AddChildCollections(int poiId, POIRequestDto dto)
    {
        foreach (var imageUrl in BuildNormalizedImageUrls(dto.MainImage, dto.Images.Select(x => x.ImageUrl)))
        {
            _context.POIImages.Add(new POIImage
            {
                POIId = poiId,
                ImageUrl = imageUrl
            });
        }

        foreach (var tr in dto.Translations)
        {
            _context.POITranslations.Add(new POITranslation
            {
                POIId = poiId,
                LanguageCode = tr.LanguageCode,
                Description = tr.Description,
                DetailedDescription = tr.DetailedDescription,
                AudioScript = tr.AudioScript,
                AudioUrl = tr.AudioUrl
            });
        }
    }

    private string? ValidatePOI(POIRequestDto dto)
    {
        if (dto.Latitude < -90 || dto.Latitude > 90)
            return "Latitude must be between -90 and 90.";

        if (dto.Longitude < -180 || dto.Longitude > 180)
            return "Longitude must be between -180 and 180.";

        var duplicateLang = dto.Translations
            .GroupBy(t => t.LanguageCode)
            .Any(g => g.Count() > 1);

        if (duplicateLang)
            return "Duplicate languageCode detected.";

        var vi = dto.Translations.FirstOrDefault(t => t.LanguageCode == "vi");
        if (vi != null &&
            (!string.IsNullOrWhiteSpace(vi.DetailedDescription) ||
             !string.IsNullOrWhiteSpace(vi.AudioScript) ||
             !string.IsNullOrWhiteSpace(vi.AudioUrl)) &&
            string.IsNullOrWhiteSpace(vi.Description))
            return "Vietnamese description cannot be empty.";

        var imageCount = BuildNormalizedImageUrls(dto.MainImage, dto.Images.Select(x => x.ImageUrl)).Count();
        if (imageCount > 10)
            return "A POI can have at most 10 images.";

        return null;
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
            return imageUrl;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return uri.PathAndQuery;
        }

        return imageUrl;
    }
}
