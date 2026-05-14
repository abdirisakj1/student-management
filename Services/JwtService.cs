using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartWasteManagement.Models;

namespace SmartWasteManagement.Services;

public interface IJwtService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}

/// <summary>
/// Builds signed JWT access tokens with user id, email, and role claims.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwt;

    public JwtService(IOptions<JwtSettings> jwtOptions)
    {
        _jwt = jwtOptions.Value;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        if (string.IsNullOrEmpty(user.Id))
            throw new InvalidOperationException("User must have an Id before issuing a token.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        var role = Roles.Normalize(user.Role);

        // Short "role" claim + RoleClaimType "role" (MapInboundClaims false) fixes [Authorize(Roles = "Admin")] with JwtBearer.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
