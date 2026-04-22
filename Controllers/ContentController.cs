using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.DTOs;
using OneSBackend.Services;

namespace OneSBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ContentService _contentService;

    public ContentController(AppDbContext context, ContentService contentService)
    {
        _context = context;
        _contentService = contentService;
    }

    [HttpGet("version")]
    public async Task<IActionResult> GetVersion()
    {
        var version = await _contentService.GetVersionAsync();
        return Ok(new { version });
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadContent()
    {
        var version = await _context.ContentVersions.FirstOrDefaultAsync();

        var pois = await _context.POIs
            .Include(p => p.Translations)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .ToListAsync();

        var poiDtos = pois.Select(p => new POIResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            DetectionRadius = p.DetectionRadius,
            Priority = p.Priority,
            MainImage = ToAbsoluteUrl(p.MainImage),
            QrCode = p.QrCode,
            IsActive = p.IsActive,
            Translations = p.Translations!.Select(t => new TranslationDto
            {
                LanguageCode = t.LanguageCode,
                Description = t.Description,
                DetailedDescription = t.DetailedDescription,
                AudioScript = t.AudioScript,
                AudioUrl = t.AudioUrl
            }).ToList(),
            Images = BuildAbsoluteImageUrls(p.MainImage, p.Images!.Select(i => i.ImageUrl)).ToList()
        }).ToList();

        var result = new ContentDownloadDto
        {
            Version = version?.Version ?? 0,
            POIs = poiDtos
        };

        return Ok(result);
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

    private IEnumerable<string> BuildAbsoluteImageUrls(string? mainImage, IEnumerable<string?>? imageUrls)
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
            var absoluteUrl = ToAbsoluteUrl(candidate);
            if (string.IsNullOrWhiteSpace(absoluteUrl) || !seen.Add(absoluteUrl))
            {
                continue;
            }

            yield return absoluteUrl;
        }
    }
}
