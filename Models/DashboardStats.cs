namespace SmartWasteManagement.Models;

public class DashboardStats
{
    public int TotalDrivers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalTrucks { get; set; }
    public int TotalCollections { get; set; }
    public int FullSmartBins { get; set; }
    public int OverflowBins { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int PendingPickups { get; set; }
    public int ActiveRoutes { get; set; }
    public int FailedCollections { get; set; }
    public List<RevenuePoint> RevenueTrend { get; set; } = new();
    public List<WasteCategoryStat> WasteByCategory { get; set; } = new();
}

public class RevenuePoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class WasteCategoryStat
{
    public string Category { get; set; } = string.Empty;
    public double Percentage { get; set; }
}
