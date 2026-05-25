using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/pickups")]
[Authorize]
public class PickupRequestsController : ControllerBase
{
    private readonly IPickupRequestService _pickups;
    private readonly INotificationService _notifications;
    private readonly IUserService _users;
    private readonly ITruckService _trucks;

    public PickupRequestsController(
        IPickupRequestService pickups,
        INotificationService notifications,
        IUserService users,
        ITruckService trucks)
    {
        _pickups = pickups;
        _notifications = notifications;
        _users = users;
        _trucks = trucks;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PickupRequest>>> GetAll()
    {
        if (User.IsInRole(Roles.Customer))
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            return Ok(await _pickups.GetByCustomerIdAsync(customerId!));
        }
        return Ok(await _pickups.GetAllAsync());
    }

    [HttpGet("driver-tasks")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<ActionResult<IEnumerable<PickupRequest>>> GetDriverTasks()
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(driverId))
            return Unauthorized();

        var driver = await _users.GetByIdAsync(driverId);
        if (driver is null)
            return Unauthorized();

        var truck = await _trucks.GetByDriverIdAsync(driverId);
        if (truck is null && !string.IsNullOrEmpty(driver.AssignedTruckId))
            truck = await _trucks.GetByIdAsync(driver.AssignedTruckId);

        var isOnline = string.Equals(driver.Status, "Online", StringComparison.OrdinalIgnoreCase)
            || string.Equals(driver.Status, "Active", StringComparison.OrdinalIgnoreCase);
        var hasActiveTruck = truck is not null
            && string.Equals(truck.Status, "Active", StringComparison.OrdinalIgnoreCase);
        var isEligible = isOnline && hasActiveTruck;

        var all = await _pickups.GetAllAsync();

        var myTasks = all.Where(p =>
            string.Equals(p.AssignedDriverId, driverId, StringComparison.Ordinal) &&
            (string.Equals(p.Status, "In Progress", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase)));

        if (!isEligible)
            return Ok(myTasks);

        var available = all.Where(p =>
            string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(p.AssignedDriverId));

        var tasks = available.Concat(myTasks).OrderByDescending(p => p.CreatedAt);
        return Ok(tasks);
    }

    [HttpGet("driver-navigation")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<ActionResult<object>> GetDriverNavigation()
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(driverId))
            return Unauthorized();

        var all = await _pickups.GetAllAsync();
        var active = all.FirstOrDefault(p =>
            string.Equals(p.AssignedDriverId, driverId, StringComparison.Ordinal) &&
            string.Equals(p.Status, "In Progress", StringComparison.OrdinalIgnoreCase));

        if (active is null)
            return Ok(new { hasTask = false });

        var customer = await _users.GetByIdAsync(active.CustomerId);
        var driverUser = await _users.GetByIdAsync(driverId);
        var truck = await _trucks.GetByDriverIdAsync(driverId);
        if (truck is null && driverUser is not null && !string.IsNullOrEmpty(driverUser.AssignedTruckId))
            truck = await _trucks.GetByIdAsync(driverUser.AssignedTruckId);

        return Ok(new
        {
            hasTask = true,
            pickupId = active.Id,
            pickupStatus = active.Status,
            destination = new
            {
                address = active.Address,
                latitude = active.Latitude,
                longitude = active.Longitude,
            },
            customer = new
            {
                fullName = customer?.FullName ?? "Customer",
                avatarUrl = customer?.AvatarUrl,
            },
            truck = truck is null
                ? null
                : new
                {
                    truck.Id,
                    truck.TruckNumber,
                    latitude = truck.Latitude,
                    longitude = truck.Longitude,
                }
        });
    }

    [HttpGet("tracking")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<object>> GetTrackingInfo()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var pickups = await _pickups.GetByCustomerIdAsync(customerId!);
        var active = pickups.FirstOrDefault(p =>
            string.Equals(p.Status, "In Progress", StringComparison.OrdinalIgnoreCase));

        if (active is null || string.IsNullOrEmpty(active.AssignedDriverId))
            return Ok(new { hasDriver = false });

        var driver = await _users.GetByIdAsync(active.AssignedDriverId);
        if (driver is null)
            return Ok(new { hasDriver = false });

        var customer = await _users.GetByIdAsync(customerId!);
        var truck = await _trucks.GetByDriverIdAsync(active.AssignedDriverId);
        if (truck is null && !string.IsNullOrEmpty(driver.AssignedTruckId))
            truck = await _trucks.GetByIdAsync(driver.AssignedTruckId);

        return Ok(new
        {
            hasDriver = true,
            pickupStatus = active.Status,
            address = active.Address,
            latitude = active.Latitude,
            longitude = active.Longitude,
            wasteType = active.WasteType,
            customer = new
            {
                fullName = customer?.FullName ?? "Customer",
                avatarUrl = customer?.AvatarUrl,
            },
            driver = new
            {
                id = driver.Id,
                fullName = driver.FullName,
                phone = driver.Phone,
                status = driver.Status,
                avatarUrl = driver.AvatarUrl,
                latitude = truck?.Latitude,
                longitude = truck?.Longitude,
            },
            truck = truck is null
                ? null
                : new
                {
                    truckNumber = truck.TruckNumber,
                    status = truck.Status,
                    area = truck.Area,
                    latitude = truck.Latitude,
                    longitude = truck.Longitude,
                }
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PickupRequest>> GetById(string id)
    {
        var request = await _pickups.GetByIdAsync(id);
        if (request is null) return NotFound();

        if (User.IsInRole(Roles.Driver))
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isAssignedToMe = string.Equals(request.AssignedDriverId, driverId, StringComparison.Ordinal);
            var isUnassignedApproved = string.Equals(request.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(request.AssignedDriverId);
            if (!isAssignedToMe && !isUnassignedApproved)
                return NotFound();
        }

        return Ok(request);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<PickupRequest>> Create([FromBody] PickupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
            return BadRequest(new { message = "Pickup address is required." });
        if (request.Latitude is null or 0 || request.Longitude is null or 0)
            return BadRequest(new { message = "Valid map location is required." });

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        request.CustomerId = customerId!;
        request.Status = "AwaitingCustomerConfirm";
        request.Id = null;
        var created = await _pickups.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}/customer-confirm")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<PickupRequest>> CustomerConfirm(string id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var request = await _pickups.GetByIdAsync(id);
        if (request is null || request.CustomerId != customerId)
            return NotFound();

        if (!string.Equals(request.Status, "AwaitingCustomerConfirm", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Request cannot be confirmed." });

        request.Status = "Pending";
        await _pickups.UpdateAsync(id, request);

        var customer = await _users.GetByIdAsync(customerId!);
        var name = customer?.FullName ?? "Customer";
        await _notifications.SendToRoleAsync(
            Roles.Admin,
            "Pickup requested",
            $"Customer {name} requested pickup collection.",
            "Pickup",
            actionUrl: "/admin/payments",
            actionText: "Charge request",
            referenceId: id,
            referenceType: "PickupRequest");

        return Ok(request);
    }

    [HttpPut("{id}/customer-cancel")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<ActionResult<PickupRequest>> CustomerCancel(string id)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var request = await _pickups.GetByIdAsync(id);
        if (request is null || request.CustomerId != customerId)
            return NotFound();

        request.Status = "Cancelled";
        await _pickups.UpdateAsync(id, request);
        return Ok(request);
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PickupRequest>> Approve(string id)
    {
        var request = await _pickups.GetByIdAsync(id);
        if (request is null) return NotFound();
        request.Status = "Approved";
        await _pickups.UpdateAsync(id, request);
        return Ok(request);
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PickupRequest>> Reject(string id)
    {
        var request = await _pickups.GetByIdAsync(id);
        if (request is null) return NotFound();
        request.Status = "Rejected";
        await _pickups.UpdateAsync(id, request);
        return Ok(request);
    }

    [HttpPut("{id}/driver-accept")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<ActionResult<PickupRequest>> DriverAccept(string id)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var request = await _pickups.GetByIdAsync(id);
        if (request is null) return NotFound();

        if (!string.IsNullOrEmpty(request.AssignedDriverId) &&
            !string.Equals(request.AssignedDriverId, driverId, StringComparison.Ordinal))
            return BadRequest(new { message = "This task has already been accepted by another driver." });

        if (!string.Equals(request.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Task is not available for acceptance." });

        var driver = await _users.GetByIdAsync(driverId!);
        if (driver is null) return Unauthorized();

        var truck = await _trucks.GetByDriverIdAsync(driverId!);
        if (truck is null && !string.IsNullOrEmpty(driver.AssignedTruckId))
            truck = await _trucks.GetByIdAsync(driver.AssignedTruckId);

        var isOnline = string.Equals(driver.Status, "Online", StringComparison.OrdinalIgnoreCase)
            || string.Equals(driver.Status, "Active", StringComparison.OrdinalIgnoreCase);
        var hasActiveTruck = truck is not null
            && string.Equals(truck.Status, "Active", StringComparison.OrdinalIgnoreCase);

        if (!isOnline || !hasActiveTruck)
            return BadRequest(new { message = "You must be online with an active assigned truck to accept tasks." });

        request.AssignedDriverId = driverId;
        request.Status = "In Progress";
        await _pickups.UpdateAsync(id, request);

        await _notifications.CompletePrimaryByReferenceAsync("PickupTask", id, "Accepted", userId: driverId);

        var driverName = driver.FullName ?? "Driver";
        await _notifications.SendAsync(
            request.CustomerId,
            "Pickup accepted",
            $"Your pickup request has been accepted by driver {driverName}",
            "Pickup",
            actionUrl: "/customer/tracking",
            actionText: "View",
            referenceId: id,
            referenceType: "PickupAccepted");

        return Ok(request);
    }

    [HttpPut("{id}/driver-decline")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<ActionResult<PickupRequest>> DriverDecline(string id)
    {
        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var request = await _pickups.GetByIdAsync(id);
        if (request is null) return NotFound();

        request.AssignedDriverId = null;
        request.Status = "Approved";
        await _pickups.UpdateAsync(id, request);
        await _notifications.HidePrimaryByReferenceAsync("PickupTask", id, driverId!);
        return Ok(request);
    }
}
