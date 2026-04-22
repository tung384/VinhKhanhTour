using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.Models;

namespace OneSBackend.Services;

public class ContentService
{
    private readonly AppDbContext _context;

    public ContentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetVersionAsync()
    {
        var version = await _context.ContentVersions.FirstOrDefaultAsync();
        return version?.Version ?? 0;
    }

    public async Task IncreaseVersionAsync()
    {
        var version = await _context.ContentVersions.FirstOrDefaultAsync();

        if (version == null)
        {
            _context.ContentVersions.Add(new ContentVersion
            {
                Version = 1,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            version.Version += 1;
            version.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}