namespace SmartWasteManagement.Models;

/// <summary>
/// Application role names used in JWT and authorization policies.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string TruckDriver = "TruckDriver";

    /// <summary>
    /// Maps MongoDB / manual values like "admin" or "user" to canonical role strings so
    /// JWT role claims match <c>[Authorize(Roles = "Admin")]</c> checks.
    /// </summary>
    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return User;

        var r = role.Trim();
        if (r.Equals(Admin, StringComparison.OrdinalIgnoreCase))
            return Admin;
        if (r.Equals(User, StringComparison.OrdinalIgnoreCase))
            return User;
        if (r.Equals(TruckDriver, StringComparison.OrdinalIgnoreCase))
            return TruckDriver;

        return r;
    }
}
