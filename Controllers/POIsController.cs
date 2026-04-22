using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.DTOs;
using OneSBackend.Models;

namespace OneSBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class POIsController : ControllerBase
{
    private readonly AppDbContext _context;

    public POIsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPOIs([FromQuery] string? lang)
    {
        var pois = await _context.POIs
            .Include(p => p.Translations)
            .Include(p => p.Images)
            .ToListAsync();

        var result = pois.Select(p =>
        {
            var translations = p.Translations ?? new List<POITranslation>();

            if (!string.IsNullOrEmpty(lang))
            {
                translations = translations
                    .Where(t => t.LanguageCode == lang)
                    .ToList();
            }

            return new POIResponseDto
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
                Translations = translations.Select(t => new TranslationDto
                {
                    LanguageCode = t.LanguageCode,
                    Description = t.Description,
                    DetailedDescription = t.DetailedDescription,
                    AudioScript = t.AudioScript,
                    AudioUrl = t.AudioUrl
                }).ToList(),
                Images = BuildAbsoluteImageUrls(p.MainImage, p.Images?.Select(i => i.ImageUrl)).ToList()
            };
        });

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
