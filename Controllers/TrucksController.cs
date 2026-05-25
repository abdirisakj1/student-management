using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

/// <summary>
/// Fleet truck CRUD and management endpoints.
/// </summary>
[ApiController]
[Route("api/trucks")]
[Authorize]
public class TrucksController : ControllerBase
{
    private readonly ITruckService _trucks;
    private readonly IUserService _users;

    public TrucksController(ITruckService trucks, IUserService users)
    {
        _trucks = trucks;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Truck>>> GetTrucks() =>
        Ok(await _trucks.GetAllAsync());

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Truck>>> GetTrucksByStatus(string status) =>
        Ok(await _trucks.GetByStatusAsync(status));

    [HttpGet("{id}")]
    public async Task<ActionResult<Truck>> GetTruck(string id)
    {
        var truck = await _trucks.GetByIdAsync(id);
        return truck is null ? NotFound() : Ok(truck);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> CreateTruck([FromBody] CreateTruckPayload payload)
    {
        var truck = new Truck
        {
            TruckNumber = payload.TruckNumber ?? string.Empty,
            DriverId = payload.DriverId,
            DriverName = payload.DriverName,
            Status = payload.Status ?? "Active",
            Area = payload.Area,
            Model = payload.Model,
            CapacityKg = payload.CapacityKg > 0 ? payload.CapacityKg : 5000,
            Latitude = payload.Latitude,
            Longitude = payload.Longitude
        };

        var created = await _trucks.CreateAsync(truck);

        if (!string.IsNullOrEmpty(payload.DriverId))
            await _trucks.AssignDriverAsync(created.Id!, payload.DriverId, payload.DriverName);

        var result = await _trucks.GetByIdAsync(created.Id!);
        return CreatedAtAction(nameof(GetTruck), new { id = created.Id }, result ?? created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> UpdateTruck(string id, [FromBody] UpdateTruckPayload payload)
    {
        var truck = await _trucks.GetByIdAsync(id);
        if (truck is null)
            return NotFound();

        if (!string.IsNullOrEmpty(payload.TruckNumber))
            truck.TruckNumber = payload.TruckNumber;
        if (!string.IsNullOrEmpty(payload.Area))
            truck.Area = payload.Area;
        if (!string.IsNullOrEmpty(payload.Model))
            truck.Model = payload.Model;
        if (!string.IsNullOrEmpty(payload.Status))
            truck.Status = payload.Status;
        if (payload.CapacityKg.HasValue && payload.CapacityKg.Value > 0)
            truck.CapacityKg = payload.CapacityKg.Value;
        if (payload.Latitude.HasValue)
            truck.Latitude = payload.Latitude.Value;
        if (payload.Longitude.HasValue)
            truck.Longitude = payload.Longitude.Value;

        var ok = await _trucks.UpdateAsync(id, truck);
        if (!ok)
            return NotFound();

        var updated = await _trucks.GetByIdAsync(id);
        return Ok(updated);
    }

    [HttpPut("{id}/assign-driver")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> AssignDriver(string id, [FromBody] AssignDriverPayload payload)
    {
        var driver = await _users.GetByIdAsync(payload.DriverId);
        if (driver is null)
            return NotFound(new { message = "Driver not found." });

        var driverName = !string.IsNullOrWhiteSpace(payload.DriverName)
            ? payload.DriverName
            : driver.FullName;

        var ok = await _trucks.AssignDriverAsync(id, payload.DriverId, driverName);
        if (!ok)
            return NotFound();

        var truck = await _trucks.GetByIdAsync(id);
        return Ok(truck);
    }

    [HttpPut("{id}/unassign-driver")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> UnassignDriver(string id)
    {
        var ok = await _trucks.UnassignDriverAsync(id);
        if (!ok)
            return NotFound();

        var truck = await _trucks.GetByIdAsync(id);
        return Ok(truck);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> UpdateStatus(string id, [FromBody] UpdateStatusPayload payload)
    {
        var ok = await _trucks.UpdateStatusAsync(id, payload.Status);
        if (!ok)
            return NotFound();

        var truck = await _trucks.GetByIdAsync(id);
        return Ok(truck);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteTruck(string id)
    {
        var ok = await _trucks.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

public class CreateTruckPayload
{
    public string? TruckNumber { get; set; }
    public string? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? Status { get; set; }

    [Required]
    [StringLength(200)]
    public string Area { get; set; } = string.Empty;

    public string? Model { get; set; }
    public int CapacityKg { get; set; } = 5000;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class UpdateTruckPayload
{
    public string? TruckNumber { get; set; }
    public string? Area { get; set; }
    public string? Model { get; set; }
    public string? Status { get; set; }
    public int? CapacityKg { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class AssignDriverPayload
{
    [Required]
    public string DriverId { get; set; } = string.Empty;

    public string? DriverName { get; set; }
}

public class UpdateStatusPayload
{
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
}
