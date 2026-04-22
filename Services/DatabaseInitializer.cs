using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.Models;

namespace OneSBackend.Services;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public DatabaseInitializer(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        await EnsureAccountsTableAsync();
        await SeedAdminAsync();
    }

    private async Task EnsureAccountsTableAsync()
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS Accounts (
                Id INT NOT NULL AUTO_INCREMENT,
                Username VARCHAR(100) NOT NULL,
                Password VARCHAR(255) NOT NULL,
                Role VARCHAR(20) NOT NULL,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                PoiId INT NULL,
                CreatedAt DATETIME(6) NOT NULL,
                UpdatedAt DATETIME(6) NOT NULL,
                CONSTRAINT PK_Accounts PRIMARY KEY (Id),
                CONSTRAINT UX_Accounts_Username UNIQUE (Username),
                CONSTRAINT UX_Accounts_PoiId UNIQUE (PoiId)
            );
            """;

        await _context.Database.ExecuteSqlRawAsync(sql);
    }

    private async Task SeedAdminAsync()
    {
        var username = _configuration["AdminAccount:Username"];
        var password = _configuration["AdminAccount:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existingAdmin = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username);
        if (existingAdmin != null)
        {
            var changed = false;

            if (existingAdmin.Role != AccountRoles.Admin)
            {
                existingAdmin.Role = AccountRoles.Admin;
                changed = true;
            }

            if (existingAdmin.Password != password)
            {
                existingAdmin.Password = password;
                changed = true;
            }

            if (!existingAdmin.IsActive)
            {
                existingAdmin.IsActive = true;
                changed = true;
            }

            if (changed)
            {
                existingAdmin.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return;
        }

        _context.Accounts.Add(new Account
        {
            Username = username,
            Password = password,
            Role = AccountRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
