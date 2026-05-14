using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

/// <summary>
/// Waste places and citizen reports.
/// </summary>
[ApiController]
[Route("api/places")]
[Authorize]
public class PlacesController : ControllerBase
{
    private readonly IPlaceService _places;

    public PlacesController(IPlaceService places)
    {
        _places = places;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Place>>> GetPlaces() =>
        Ok(await _places.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Place>> GetPlace(string id)
    {
        var place = await _places.GetByIdAsync(id);
        return place is null ? NotFound() : Ok(place);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.User},{Roles.Admin},{Roles.TruckDriver}")]
    public async Task<ActionResult<Place>> CreatePlace([FromBody] Place place)
    {
        place.Id = null;
        var created = await _places.CreateAsync(place);
        return CreatedAtAction(nameof(GetPlace), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Place>> UpdatePlace(string id, [FromBody] Place place)
    {
        var ok = await _places.UpdateAsync(id, place);
        if (!ok)
            return NotFound();
        var updated = await _places.GetByIdAsync(id);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeletePlace(string id)
    {
        var ok = await _places.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
