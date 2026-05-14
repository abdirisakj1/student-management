using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

/// <summary>
/// Fleet truck CRUD.
/// </summary>
[ApiController]
[Route("api/trucks")]
[Authorize]
public class TrucksController : ControllerBase
{
    private readonly ITruckService _trucks;

    public TrucksController(ITruckService trucks)
    {
        _trucks = trucks;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Truck>>> GetTrucks() =>
        Ok(await _trucks.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Truck>> GetTruck(string id)
    {
        var truck = await _trucks.GetByIdAsync(id);
        return truck is null ? NotFound() : Ok(truck);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> CreateTruck([FromBody] Truck truck)
    {
        truck.Id = null;
        var created = await _trucks.CreateAsync(truck);
        return CreatedAtAction(nameof(GetTruck), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Truck>> UpdateTruck(string id, [FromBody] Truck truck)
    {
        var ok = await _trucks.UpdateAsync(id, truck);
        if (!ok)
            return NotFound();
        var updated = await _trucks.GetByIdAsync(id);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteTruck(string id)
    {
        var ok = await _trucks.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
