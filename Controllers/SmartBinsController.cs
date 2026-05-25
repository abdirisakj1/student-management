using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartWasteManagement.Hubs;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/smartbins")]
[Authorize]
public class SmartBinsController : ControllerBase
{
    private readonly ISmartBinService _bins;
    private readonly IHubContext<LiveTrackingHub> _hub;

    public SmartBinsController(ISmartBinService bins, IHubContext<LiveTrackingHub> hub)
    {
        _bins = bins;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmartBin>>> GetAll() =>
        Ok(await _bins.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<SmartBin>> Get(string id)
    {
        var bin = await _bins.GetByIdAsync(id);
        return bin is null ? NotFound() : Ok(bin);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<SmartBin>> Create([FromBody] SmartBin bin)
    {
        bin.Id = null;
        var created = await _bins.CreateAsync(bin);
        await _hub.Clients.Group("tracking").SendAsync("BinFillUpdated", new
        {
            binId = created.Id,
            fillLevelPercent = created.FillLevelPercent,
            isOverflow = created.IsOverflow
        });
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
    public async Task<ActionResult<SmartBin>> Update(string id, [FromBody] SmartBin bin)
    {
        var ok = await _bins.UpdateAsync(id, bin);
        if (!ok) return NotFound();
        var updated = await _bins.GetByIdAsync(id);
        if (updated is not null)
        {
            await _hub.Clients.Group("tracking").SendAsync("BinFillUpdated", new
            {
                binId = updated.Id,
                fillLevelPercent = updated.FillLevelPercent,
                isOverflow = updated.IsOverflow
            });
        }
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id) =>
        await _bins.DeleteAsync(id) ? NoContent() : NotFound();
}
