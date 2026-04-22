using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSBackend.Data;
using OneSBackend.DTOs;
using OneSBackend.Services;

namespace OneSBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _tokenService;

    public AuthController(AppDbContext context, JwtTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var username = request.Username.Trim();
        var password = request.Password.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == username && a.Password == password);
        if (account == null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        if (!account.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = "ACCOUNT_INACTIVE",
                message = "You must pay before continue access the page."
            });
        }

        return Ok(new LoginResponseDto
        {
            Token = _tokenService.CreateToken(account),
            Username = account.Username,
            Role = account.Role,
            AccountId = account.Id,
            PoiId = account.PoiId
        });
    }
}
