namespace OneSBackend.DTOs;

public class OwnerAccountCreateDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string StallName { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class AccountStatusUpdateDto
{
    public bool IsActive { get; set; }
}

public class AccountPasswordUpdateDto
{
    public string Password { get; set; } = string.Empty;
}

public class AccountResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? PoiId { get; set; }
    public string? PoiName { get; set; }
    public DateTime CreatedAt { get; set; }
}
