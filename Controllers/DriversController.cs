using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize(Roles = Roles.Admin)]
public class DriversController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IDriverService _drivers;

    public DriversController(IUserService users, IDriverService drivers)
    {
        _users = users;
        _drivers = drivers;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummary>>> GetDrivers()
    {
        var drivers = await _drivers.GetAllDriversAsync();
        return Ok(drivers.Select(UserSummary.FromUser));
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<UserSummary>>> GetActiveDrivers()
    {
        var drivers = await _drivers.GetActiveDriversAsync();
        return Ok(drivers.Select(UserSummary.FromUser));
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<UserSummary>>> GetDriversByStatus(string status)
    {
        var drivers = await _drivers.GetDriversByStatusAsync(status);
        return Ok(drivers.Select(UserSummary.FromUser));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserSummary>> GetDriver(string id)
    {
        var driver = await _drivers.GetDriverByIdAsync(id);
        if (driver is null)
            return NotFound();
        return Ok(UserSummary.FromUser(driver));
    }

    [HttpPost]
    public async Task<ActionResult<UserSummary>> CreateDriver([FromBody] CreateDriverRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Full name is required." });
        
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });
        
        if (await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant()) is not null)
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Roles.Driver,
            Phone = request.Phone,
            LicenseNumber = request.LicenseNumber,
            AssignedTruckId = request.AssignedTruckId,
            AssignedRouteId = request.AssignedRouteId,
            Status = request.Status ?? "Online",
            IsActive = true
        };
        await _users.CreateAsync(user);
        return CreatedAtAction(nameof(GetDriver), new { id = user.Id }, UserSummary.FromUser(user));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserSummary>> UpdateDriver(string id, [FromBody] UpdateDriverRequest request)
    {
        var existing = await _drivers.GetDriverByIdAsync(id);
        if (existing is null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.FullName))
            existing.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.Email))
            existing.Email = request.Email.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(request.Phone))
            existing.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.LicenseNumber))
            existing.LicenseNumber = request.LicenseNumber;
        if (!string.IsNullOrEmpty(request.Status))
            existing.Status = request.Status;
        if (!string.IsNullOrEmpty(request.AssignedTruckId))
            existing.AssignedTruckId = request.AssignedTruckId;
        if (!string.IsNullOrEmpty(request.AssignedRouteId))
            existing.AssignedRouteId = request.AssignedRouteId;
        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(request.Password))
            existing.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _users.UpdateAsync(id, existing);
        return Ok(UserSummary.FromUser(existing));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<UserSummary>> UpdateStatus(string id, [FromBody] UpdateDriverStatusPayload payload)
    {
        var ok = await _drivers.UpdateDriverStatusAsync(id, payload.Status);
        if (!ok)
            return NotFound();

        var driver = await _drivers.GetDriverByIdAsync(id);
        return Ok(UserSummary.FromUser(driver!));
    }

    [HttpPut("{id}/assign-route/{routeId}")]
    public async Task<ActionResult<UserSummary>> AssignRoute(string id, string routeId)
    {
        var ok = await _drivers.AssignRouteToDriverAsync(id, routeId);
        if (!ok)
            return NotFound();

        var driver = await _drivers.GetDriverByIdAsync(id);
        return Ok(UserSummary.FromUser(driver!));
    }

    [HttpPut("{id}/unassign-route")]
    public async Task<ActionResult<UserSummary>> UnassignRoute(string id)
    {
        var ok = await _drivers.UnassignRouteFromDriverAsync(id);
        if (!ok)
            return NotFound();

        var driver = await _drivers.GetDriverByIdAsync(id);
        return Ok(UserSummary.FromUser(driver!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDriver(string id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null || !Roles.IsDriver(user.Role))
            return NotFound();
        var ok = await _users.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

public class CreateDriverRequest
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

    [StringLength(50)]
    public string? LicenseNumber { get; set; }

    public string? AssignedTruckId { get; set; }
    public string? AssignedRouteId { get; set; }
    public string? Status { get; set; }
}

public class UpdateDriverRequest
{
    [StringLength(200)]
    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(50)]
    public string? LicenseNumber { get; set; }

    public string? AssignedTruckId { get; set; }
    public string? AssignedRouteId { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    [StringLength(200, MinimumLength = 6)]
    public string? Password { get; set; }

    public bool? IsActive { get; set; }
}

public class UpdateDriverStatusPayload
{
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
}
