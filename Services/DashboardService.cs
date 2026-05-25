using SmartWasteManagement.Models;

namespace SmartWasteManagement.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly IUserService _users;
    private readonly ITruckService _trucks;
    private readonly ISmartBinService _bins;
    private readonly ICollectionService _collections;
    private readonly IPickupRequestService _pickups;
    private readonly IRouteService _routes;
    private readonly IPaymentService _payments;

    public DashboardService(
        IUserService users,
        ITruckService trucks,
        ISmartBinService bins,
        ICollectionService collections,
        IPickupRequestService pickups,
        IRouteService routes,
        IPaymentService payments)
    {
        _users = users;
        _trucks = trucks;
        _bins = bins;
        _collections = collections;
        _pickups = pickups;
        _routes = routes;
        _payments = payments;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var users = await _users.GetAllAsync();
        var trucks = await _trucks.GetAllAsync();
        var bins = await _bins.GetAllAsync();
        var collections = await _collections.GetAllAsync();
        var pickups = await _pickups.GetAllAsync();
        var routes = await _routes.GetAllAsync();
        var payments = await _payments.GetAllAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new DashboardStats
        {
            TotalDrivers = users.Count(u => Roles.IsDriver(u.Role)),
            TotalCustomers = users.Count(u => Roles.IsCustomer(u.Role)),
            TotalTrucks = trucks.Count,
            TotalCollections = collections.Count,
            FullSmartBins = bins.Count(b => b.FillLevelPercent >= 80),
            OverflowBins = bins.Count(b => b.IsOverflow),
            MonthlyRevenue = payments
                .Where(p => p.PaidAt >= monthStart && p.Status == "Paid")
                .Sum(p => p.Amount),
            PendingPickups = pickups.Count(p => p.Status == "Pending"),
            ActiveRoutes = routes.Count(r => r.Status is "InProgress" or "Active"),
            FailedCollections = collections.Count(c => c.Status == "Failed"),
            RevenueTrend = BuildRevenueTrend(payments),
            WasteByCategory = new List<WasteCategoryStat>
            {
                new() { Category = "General", Percentage = 45 },
                new() { Category = "Recyclable", Percentage = 30 },
                new() { Category = "Organic", Percentage = 15 },
                new() { Category = "Hazardous", Percentage = 10 }
            }
        };
    }

    private static List<RevenuePoint> BuildRevenueTrend(List<Payment> payments)
    {
        var labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
        var year = DateTime.UtcNow.Year;
        return labels.Select((label, i) => new RevenuePoint
        {
            Label = label,
            Amount = payments
                .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Year == year && p.PaidAt.Value.Month == i + 1 && p.Status == "Paid")
                .Sum(p => p.Amount)
        }).ToList();
    }
}
