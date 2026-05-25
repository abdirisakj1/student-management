using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

/// <summary>
/// Read-only lookups shared across roles (avoids Admin-only customer/driver list endpoints).
/// </summary>
[ApiController]
[Route("api/lookup")]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IPickupRequestService _pickups;

    public LookupController(IUserService users, IPickupRequestService pickups)
    {
        _users = users;
        _pickups = pickups;
    }

    [HttpGet("customers")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
    public async Task<ActionResult<IEnumerable<object>>> GetCustomers()
    {
        var customers = await _users.GetByRoleAsync(Roles.Customer);
        return Ok(customers.Select(c => new
        {
            id = c.Id,
            fullName = c.FullName,
            email = c.Email,
            phone = c.Phone
        }));
    }

    /// <summary>
    /// Drivers who accepted this customer's pickup (for "Driver Behavior" complaints).
    /// </summary>
    [HttpGet("drivers-for-complaint")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<IEnumerable<object>>> GetDriversForComplaint()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var pickups = await _pickups.GetByCustomerIdAsync(customerId!);
        var driverIds = pickups
            .Where(p => !string.IsNullOrEmpty(p.AssignedDriverId))
            .Select(p => p.AssignedDriverId!)
            .Distinct()
            .ToList();

        var result = new List<object>();
        foreach (var driverId in driverIds)
        {
            var driver = await _users.GetByIdAsync(driverId);
            if (driver is not null && Roles.IsDriver(driver.Role))
                result.Add(new { id = driver.Id, fullName = driver.FullName });
        }

        return Ok(result);
    }
}
