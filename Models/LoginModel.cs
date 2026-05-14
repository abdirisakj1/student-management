using System.ComponentModel.DataAnnotations;

namespace SmartWasteManagement.Models;

/// <summary>
/// Login request payload.
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Registration request payload. New accounts are created with role User.
/// </summary>
public class RegisterRequest
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }
}

/// <summary>
/// JWT and user summary returned after successful login or register.
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserSummary User { get; set; } = null!;
}

/// <summary>
/// User fields exposed to clients (no password).
/// </summary>
public class UserSummary
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public static UserSummary FromUser(User user) =>
        new()
        {
            Id = user.Id ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email,
            Role = Roles.Normalize(user.Role),
            Phone = user.Phone
        };
}
