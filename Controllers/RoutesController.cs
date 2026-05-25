using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly IRouteService _routes;

    public RoutesController(IRouteService routes) => _routes = routes;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoutePlan>>> GetAll()
    {
        if (User.IsInRole(Roles.Driver))
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(driverId))
                return Unauthorized();
            return Ok(await _routes.GetByDriverIdAsync(driverId));
        }
        return Ok(await _routes.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoutePlan>> Get(string id)
    {
        var route = await _routes.GetByIdAsync(id);
        return route is null ? NotFound() : Ok(route);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RoutePlan>> Create([FromBody] RoutePlan route)
    {
        route.Id = null;
        var created = await _routes.CreateAsync(route);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
    public async Task<ActionResult<RoutePlan>> Update(string id, [FromBody] RoutePlan route)
    {
        var ok = await _routes.UpdateAsync(id, route);
        if (!ok) return NotFound();
        var updated = await _routes.GetByIdAsync(id);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id) =>
        await _routes.DeleteAsync(id) ? NoContent() : NotFound();
}
