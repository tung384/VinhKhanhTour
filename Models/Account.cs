namespace OneSBackend.Models;

public class Account
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = AccountRoles.Owner;
    public bool IsActive { get; set; } = true;
    public int? PoiId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public POI? Poi { get; set; }
}

public static class AccountRoles
{
    public const string Admin = "Admin";
    public const string Owner = "Owner";
}
