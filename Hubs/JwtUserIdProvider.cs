using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SmartWasteManagement.Hubs;

/// <summary>
/// Maps SignalR user targets to JWT "sub" (user id) for Clients.User(...).
/// </summary>
public class JwtUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("sub")?.Value
        ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
