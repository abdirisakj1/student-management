using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ICloudinaryService _cloudinary;

    public ProfileController(IUserService users, ICloudinaryService cloudinary)
    {
        _users = users;
        _cloudinary = cloudinary;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var user = await _users.GetByIdAsync(userId!);
        if (user is null) return NotFound();
        return Ok(ProfileDto.FromUser(user));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var user = await _users.GetByIdAsync(userId!);
        if (user is null) return NotFound();

        var role = Roles.Normalize(user.Role);

        if (role == Roles.Admin || role == Roles.Customer)
        {
            if (!string.IsNullOrWhiteSpace(request.Username))
                user.Username = request.Username.Trim();
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName.Trim();
        }

        if (role == Roles.Customer && request.Address is not null)
            user.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (request.AvatarUrl is not null)
            user.AvatarUrl = request.AvatarUrl;

        await _users.UpdateAsync(userId!, user);
        return Ok(ProfileDto.FromUser(user));
    }

    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ProfileDto>> UploadAvatar(IFormFile file)
    {
        if (!_cloudinary.IsConfigured)
            return StatusCode(503, new { message = "Cloudinary is not configured. Set CLOUDINARY_URL in .env." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var user = await _users.GetByIdAsync(userId!);
        if (user is null) return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
            return BadRequest(new { message = "Only image files are allowed." });

        await using var stream = file.OpenReadStream();
        var folder = $"smart-waste/avatars/{Roles.Normalize(user.Role).ToLowerInvariant()}";
        var url = await _cloudinary.UploadImageAsync(stream, file.FileName, folder);
        if (string.IsNullOrEmpty(url))
            return StatusCode(500, new { message = "Failed to upload image." });

        user.AvatarUrl = url;
        await _users.UpdateAsync(userId!, user);
        return Ok(ProfileDto.FromUser(user));
    }
}

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public bool CanEditUsername { get; set; }
    public bool CanEditEmail { get; set; }
    public bool CanEditPassword { get; set; }
    public bool CanEditAddress { get; set; }

    public static ProfileDto FromUser(User user)
    {
        var role = Roles.Normalize(user.Role);
        return new ProfileDto
        {
            Id = user.Id ?? string.Empty,
            FullName = user.FullName,
            Username = user.Username ?? user.FullName,
            Email = user.Email,
            Role = role,
            Phone = user.Phone,
            Address = user.Address,
            AvatarUrl = user.AvatarUrl,
            CanEditUsername = role is Roles.Admin or Roles.Customer,
            CanEditEmail = false,
            CanEditPassword = true,
            CanEditAddress = role == Roles.Customer
        };
    }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
}
