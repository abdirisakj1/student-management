using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collections;
    private readonly IPickupRequestService _pickups;
    private readonly ICloudinaryService _cloudinary;
    private readonly ITruckService _trucks;

    public CollectionsController(
        ICollectionService collections,
        IPickupRequestService pickups,
        ICloudinaryService cloudinary,
        ITruckService trucks)
    {
        _collections = collections;
        _pickups = pickups;
        _cloudinary = cloudinary;
        _trucks = trucks;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CollectionRecord>>> GetAll()
    {
        if (User.IsInRole(Roles.Driver))
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(driverId))
                return Unauthorized();
            return Ok(await _collections.GetByDriverIdAsync(driverId));
        }
        return Ok(await _collections.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionRecord>> GetById(string id)
    {
        var record = await _collections.GetByIdAsync(id);
        if (record is null)
            return NotFound();
        return Ok(record);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
    public async Task<ActionResult<CollectionRecord>> Create([FromBody] CreateCollectionPayload payload)
    {
        if (User.IsInRole(Roles.Driver))
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            payload.DriverId = driverId;
        }

        string? truckId = payload.TruckId;
        if (User.IsInRole(Roles.Driver) && string.IsNullOrEmpty(truckId))
        {
            var driverId = payload.DriverId;
            if (!string.IsNullOrEmpty(driverId))
            {
                var truck = await _trucks.GetByDriverIdAsync(driverId);
                truckId = truck?.Id;
            }
        }

        var record = new CollectionRecord
        {
            RouteId = payload.RouteId,
            DriverId = payload.DriverId,
            TruckId = truckId,
            PickupRequestId = payload.PickupRequestId,
            Location = payload.Location,
            Status = payload.Status ?? "Completed",
            WeightKg = payload.WeightKg,
            ProofPhotoUrl = payload.ProofPhotoUrl,
            Notes = payload.Notes,
            CollectedAt = payload.CollectedAt ?? DateTime.UtcNow
        };

        var created = await _collections.CreateAsync(record);

        if (!string.IsNullOrEmpty(payload.PickupRequestId))
        {
            var pickup = await _pickups.GetByIdAsync(payload.PickupRequestId);
            if (pickup is not null)
            {
                pickup.Status = "Completed";
                await _pickups.UpdateAsync(payload.PickupRequestId, pickup);
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id}/upload-photo")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<ActionResult<CollectionRecord>> UploadPhoto(string id, IFormFile file)
    {
        var record = await _collections.GetByIdAsync(id);
        if (record is null)
            return NotFound();

        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (record.DriverId != driverId)
            return Forbid("Cannot modify collection records of other drivers.");

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { message = "Only image files are allowed." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File size must not exceed 5MB." });

        string? photoUrl;
        if (_cloudinary.IsConfigured)
        {
            await using var stream = file.OpenReadStream();
            photoUrl = await _cloudinary.UploadImageAsync(stream, file.FileName, "smart-waste/collections");
            if (string.IsNullOrEmpty(photoUrl))
                return StatusCode(500, new { message = "Failed to upload proof photo to Cloudinary." });
        }
        else
        {
            return StatusCode(503, new { message = "Cloudinary is not configured. Set CLOUDINARY_URL in .env." });
        }

        record.ProofPhotoUrl = photoUrl;
        await _collections.UpdateAsync(id, record);

        var updated = await _collections.GetByIdAsync(id);
        return Ok(updated);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Driver)]
    public async Task<ActionResult<CollectionRecord>> Update(string id, [FromBody] UpdateCollectionPayload payload)
    {
        var existing = await _collections.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (existing.DriverId != driverId)
            return Forbid("Cannot modify collection records of other drivers.");

        if (!string.IsNullOrEmpty(payload.Location))
            existing.Location = payload.Location;
        if (!string.IsNullOrEmpty(payload.Status))
            existing.Status = payload.Status;
        if (payload.WeightKg.HasValue && payload.WeightKg.Value > 0)
            existing.WeightKg = payload.WeightKg.Value;
        if (!string.IsNullOrEmpty(payload.Notes))
            existing.Notes = payload.Notes;

        var ok = await _collections.UpdateAsync(id, existing);
        if (!ok)
            return NotFound();

        var updated = await _collections.GetByIdAsync(id);
        return Ok(updated);
    }
}

public class CreateCollectionPayload
{
    public string? RouteId { get; set; }
    public string? DriverId { get; set; }
    public string? TruckId { get; set; }

    [Required]
    [StringLength(300)]
    public string Location { get; set; } = string.Empty;

    public string? Status { get; set; }

    [Required]
    [Range(0.1, double.MaxValue)]
    public double WeightKg { get; set; }

    public string? ProofPhotoUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime? CollectedAt { get; set; }
    public string? PickupRequestId { get; set; }
}

public class UpdateCollectionPayload
{
    public string? Location { get; set; }
    public string? Status { get; set; }
    public double? WeightKg { get; set; }
    public string? Notes { get; set; }
}
