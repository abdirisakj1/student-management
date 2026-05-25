using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IJwtService _jwt;
    private readonly IRefreshTokenService _refreshTokens;

    public AuthController(IUserService users, IJwtService jwt, IRefreshTokenService refreshTokens)
    {
        _users = users;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant()) is not null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Roles.Customer,
            Phone = request.Phone,
            Address = request.Address
        };

        await _users.CreateAsync(user);
        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Account is deactivated." });

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var stored = await _refreshTokens.GetValidAsync(request.RefreshToken);
        if (stored is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await _users.GetByIdAsync(stored.UserId);
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        await _refreshTokens.RevokeAsync(stored.Id!);
        return Ok(await BuildAuthResponseAsync(user));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var (token, expires) = _jwt.CreateToken(user);
        var (refresh, refreshExpires) = await _refreshTokens.CreateAsync(user.Id!);
        return new AuthResponse
        {
            Token = token,
            RefreshToken = refresh,
            ExpiresAt = expires,
            RefreshExpiresAt = refreshExpires,
            User = UserSummary.FromUser(user)
        };
    }
}
