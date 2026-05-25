namespace SmartWasteManagement.Models;

/// <summary>
/// Application role names used in JWT and authorization policies.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Driver = "Driver";
    public const string Customer = "Customer";

    // Legacy aliases (existing MongoDB documents)
    public const string User = "User";
    public const string TruckDriver = "TruckDriver";

    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Customer;

        var r = role.Trim();
        if (r.Equals(Admin, StringComparison.OrdinalIgnoreCase))
            return Admin;
        if (r.Equals(Driver, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(TruckDriver, StringComparison.OrdinalIgnoreCase))
            return Driver;
        if (r.Equals(Customer, StringComparison.OrdinalIgnoreCase) ||
            r.Equals(User, StringComparison.OrdinalIgnoreCase))
            return Customer;

        return r;
    }

    public static bool IsAdmin(string? role) => Normalize(role) == Admin;
    public static bool IsDriver(string? role) => Normalize(role) == Driver;
    public static bool IsCustomer(string? role) => Normalize(role) == Customer;
}
