using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/complaints")]
[Authorize]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaints;
    private readonly INotificationService _notifications;
    private readonly IUserService _users;

    public ComplaintsController(IComplaintService complaints, INotificationService notifications, IUserService users)
    {
        _complaints = complaints;
        _notifications = notifications;
        _users = users;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var list = await _complaints.GetAllAsync();
        var result = new List<object>();
        foreach (var c in list)
        {
            var customer = await _users.GetByIdAsync(c.CustomerId);
            string? driverName = null;
            if (!string.IsNullOrEmpty(c.DriverId))
            {
                var driver = await _users.GetByIdAsync(c.DriverId);
                driverName = driver?.FullName;
            }
            result.Add(new
            {
                c.Id,
                c.CustomerId,
                customerName = customer?.FullName ?? "Unknown",
                c.DriverId,
                driverName,
                c.Category,
                c.Title,
                description = c.Description,
                c.Status,
                c.CreatedAt
            });
        }
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<Complaint>> Create([FromBody] CreateComplaintPayload payload)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var complaint = new Complaint
        {
            CustomerId = customerId!,
            DriverId = payload.DriverId,
            Category = payload.Category,
            Title = payload.Title,
            Description = payload.Description,
            Status = "Pending"
        };

        var created = await _complaints.CreateAsync(complaint);

        var customer = await _users.GetByIdAsync(customerId!);
        var name = customer?.FullName ?? "Customer";
        await _notifications.SendToRoleAsync(
            Roles.Admin,
            "New complaint",
            $"Customer {name} submitted a complaint: {payload.Category}.",
            "Complaint",
            actionUrl: "/admin/complaints",
            actionText: "View");

        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpGet("mine")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<IEnumerable<object>>> GetMine()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var list = await _complaints.GetByCustomerIdAsync(customerId!);
        var result = new List<object>();
        foreach (var c in list)
        {
            string? driverName = null;
            if (!string.IsNullOrEmpty(c.DriverId))
            {
                var driver = await _users.GetByIdAsync(c.DriverId);
                driverName = driver?.FullName;
            }
            result.Add(new
            {
                c.Id,
                c.Title,
                driverName,
                c.Status,
                c.CreatedAt
            });
        }
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> UpdateStatus(string id, [FromBody] UpdateComplaintStatusPayload payload)
    {
        var complaint = (await _complaints.GetAllAsync()).FirstOrDefault(c => c.Id == id);
        if (complaint is null)
            return NotFound();

        var ok = await _complaints.UpdateStatusAsync(id, payload.Status);
        if (!ok)
            return NotFound();

        await _notifications.SendAsync(
            complaint.CustomerId,
            "Complaint updated",
            $"Your complaint has been {payload.Status}.",
            "Complaint",
            actionUrl: "/customer/complaints",
            actionText: "View",
            referenceId: id,
            referenceType: "Complaint");

        return NoContent();
    }
}

public class CreateComplaintPayload
{
    [Required]
    public string Category { get; set; } = "General";

    [Required]
    [StringLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public string? DriverId { get; set; }
}

public class UpdateComplaintStatusPayload
{
    [Required]
    public string Status { get; set; } = "Open";
}
