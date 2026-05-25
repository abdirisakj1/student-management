using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Hubs;

[Authorize]
public class LiveTrackingHub : Hub
{
    private readonly ITruckService _trucks;

    public LiveTrackingHub(ITruckService trucks) => _trucks = trucks;

    public async Task JoinTrackingGroup() =>
        await Groups.AddToGroupAsync(Context.ConnectionId, "tracking");

    public async Task UpdateDriverLocation(double latitude, double longitude)
    {
        var driverId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        if (string.IsNullOrEmpty(driverId))
            return;

        var truck = await _trucks.GetByDriverIdAsync(driverId);
        if (truck is not null && !string.IsNullOrEmpty(truck.Id))
        {
            truck.Latitude = latitude;
            truck.Longitude = longitude;
            truck.UpdatedAt = DateTime.UtcNow;
            await _trucks.UpdateAsync(truck.Id, truck);
        }

        await Clients.Group("tracking").SendAsync("DriverLocationUpdated", new
        {
            driverId,
            truckId = truck?.Id,
            latitude,
            longitude,
            updatedAt = DateTime.UtcNow
        });
    }

    public async Task UpdateTruckLocation(string truckId, double latitude, double longitude) =>
        await Clients.Group("tracking").SendAsync("TruckLocationUpdated", new
        {
            truckId,
            latitude,
            longitude,
            updatedAt = DateTime.UtcNow
        });

    public async Task UpdateBinFillLevel(string binId, int fillLevelPercent) =>
        await Clients.Group("tracking").SendAsync("BinFillUpdated", new
        {
            binId,
            fillLevelPercent,
            isOverflow = fillLevelPercent >= 90,
            updatedAt = DateTime.UtcNow
        });

    public async Task SendNotification(string userId, string title, string message) =>
        await Clients.User(userId).SendAsync("NotificationReceived", new { title, message });
}
