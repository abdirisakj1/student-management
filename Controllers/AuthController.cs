using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

/// <summary>
/// Registration and JWT login.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IJwtService _jwt;

    public AuthController(IUserService users, IJwtService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    /// <summary>Creates a new user account with role User.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (await _users.GetByEmailAsync(request.Email) is not null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Roles.User,
            Phone = request.Phone
        };

        await _users.CreateAsync(user);

        var (token, expires) = _jwt.CreateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expires,
            User = UserSummary.FromUser(user)
        });
    }

    /// <summary>Authenticates a user and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var (token, expires) = _jwt.CreateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expires,
            User = UserSummary.FromUser(user)
        });
    }
}
