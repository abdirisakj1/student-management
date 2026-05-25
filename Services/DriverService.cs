using MongoDB.Driver;
using SmartWasteManagement.Models;

namespace SmartWasteManagement.Services;

public interface IDriverService
{
    Task<List<User>> GetAllDriversAsync();
    Task<List<User>> GetActiveDriversAsync();
    Task<User?> GetDriverByIdAsync(string id);
    Task<User?> GetDriverByEmailAsync(string email);
    Task<List<User>> GetDriversByStatusAsync(string status);
    Task<bool> UpdateDriverStatusAsync(string driverId, string status);
    Task<bool> AssignRouteToDriverAsync(string driverId, string routeId);
    Task<bool> UnassignRouteFromDriverAsync(string driverId);
}

/// <summary>
/// Driver management service with role-specific operations.
/// </summary>
public class DriverService : IDriverService
{
    private readonly IUserService _users;

    public DriverService(IUserService users)
    {
        _users = users;
    }

    public async Task<List<User>> GetAllDriversAsync()
    {
        return await _users.GetByRoleAsync(Roles.Driver);
    }

    public async Task<List<User>> GetActiveDriversAsync()
    {
        var drivers = await GetAllDriversAsync();
        return drivers.Where(d => d.IsActive).ToList();
    }

    public async Task<User?> GetDriverByIdAsync(string id)
    {
        var user = await _users.GetByIdAsync(id);
        return user is not null && Roles.IsDriver(user.Role) ? user : null;
    }

    public async Task<User?> GetDriverByEmailAsync(string email)
    {
        var user = await _users.GetByEmailAsync(email);
        return user is not null && Roles.IsDriver(user.Role) ? user : null;
    }

    public async Task<List<User>> GetDriversByStatusAsync(string status)
    {
        var drivers = await GetAllDriversAsync();
        return drivers.Where(d => d.Status == status).ToList();
    }

    public async Task<bool> UpdateDriverStatusAsync(string driverId, string status)
    {
        var driver = await GetDriverByIdAsync(driverId);
        if (driver is null)
            return false;

        driver.Status = status;
        return await _users.UpdateAsync(driverId, driver);
    }

    public async Task<bool> AssignRouteToDriverAsync(string driverId, string routeId)
    {
        var driver = await GetDriverByIdAsync(driverId);
        if (driver is null)
            return false;

        driver.AssignedRouteId = routeId;
        return await _users.UpdateAsync(driverId, driver);
    }

    public async Task<bool> UnassignRouteFromDriverAsync(string driverId)
    {
        var driver = await GetDriverByIdAsync(driverId);
        if (driver is null)
            return false;

        driver.AssignedRouteId = null;
        return await _users.UpdateAsync(driverId, driver);
    }
}
