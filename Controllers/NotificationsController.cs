using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) =>
        _notifications = notifications;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Ok(await _notifications.GetByUserIdAsync(userId!));
    }

    [HttpGet("unread")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetUnread()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Ok(await _notifications.GetUnreadByUserIdAsync(userId!));
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        var ok = await _notifications.MarkReadAsync(id, userId!);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        var ok = await _notifications.MarkAllReadAsync(userId!);
        return ok ? NoContent() : BadRequest();
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Notification>> Send([FromBody] SendNotificationPayload payload)
    {
        var notification = await _notifications.SendAsync(
            payload.UserId,
            payload.Title,
            payload.Message,
            payload.Type ?? "Info"
        );
        return CreatedAtAction(nameof(GetMine), notification);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _notifications.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

public class SendNotificationPayload
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    public string? Type { get; set; }
}
