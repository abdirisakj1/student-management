using SmartWasteManagement.Models;

namespace SmartWasteManagement.Services;

public interface IDataSeedService
{
    Task SeedAsync();
}

public class DataSeedService : IDataSeedService
{
    private readonly IUserService _users;
    private readonly ITruckService _trucks;
    private readonly ISmartBinService _bins;
    private readonly IRouteService _routes;
    private readonly ICollectionService _collections;
    private readonly IPaymentService _payments;

    public DataSeedService(
        IUserService users,
        ITruckService trucks,
        ISmartBinService bins,
        IRouteService routes,
        ICollectionService collections,
        IPaymentService payments)
    {
        _users = users;
        _trucks = trucks;
        _bins = bins;
        _routes = routes;
        _collections = collections;
        _payments = payments;
    }

    public async Task SeedAsync()
    {
        if ((await _users.GetAllAsync()).Count > 0)
            return;

        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@smartwaste.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = Roles.Admin,
            Phone = "+252610000001"
        };
        await _users.CreateAsync(admin);

        var driver = new User
        {
            FullName = "Ahmed Hassan",
            Email = "driver@smartwaste.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Driver@123"),
            Role = Roles.Driver,
            Phone = "+252610000002",
            LicenseNumber = "DRV-2024-001",
            Status = "Online"
        };
        await _users.CreateAsync(driver);

        var customer = new User
        {
            FullName = "Fatima Ali",
            Email = "customer@smartwaste.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
            Role = Roles.Customer,
            Phone = "+252610000003",
            Address = "Hodan District, Mogadishu"
        };
        await _users.CreateAsync(customer);

        var truck = new Truck
        {
            TruckNumber = "SWT-001",
            DriverName = driver.FullName,
            DriverId = driver.Id,
            Status = "Active",
            Area = "Hodan",
            Model = "Isuzu NQR",
            CapacityKg = 8000,
            Latitude = 2.0469,
            Longitude = 45.3182,
            LastMaintenance = DateTime.UtcNow.AddDays(-30),
            NextMaintenance = DateTime.UtcNow.AddDays(60)
        };
        await _trucks.CreateAsync(truck);

        driver.AssignedTruckId = truck.Id;
        await _users.UpdateAsync(driver.Id!, driver);

        await _bins.CreateAsync(new SmartBin
        {
            BinCode = "BIN-HOD-01",
            Location = "Hodan Market",
            Latitude = 2.0475,
            Longitude = 45.3190,
            FillLevelPercent = 92,
            Status = "Critical"
        });
        await _bins.CreateAsync(new SmartBin
        {
            BinCode = "BIN-WAD-02",
            Location = "Wadajir Center",
            Latitude = 2.0350,
            Longitude = 45.3050,
            FillLevelPercent = 45,
            Status = "Normal"
        });

        var route = new RoutePlan
        {
            Name = "Morning Hodan Route",
            Description = "Primary collection loop",
            AssignedDriverId = driver.Id,
            AssignedTruckId = truck.Id,
            StopLocations = new List<string> { "Hodan Market", "KM4 Junction", "Bakaaraha" },
            Status = "InProgress",
            ProgressPercent = 35,
            ScheduledDate = DateTime.UtcNow.Date
        };
        await _routes.CreateAsync(route);

        await _collections.CreateAsync(new CollectionRecord
        {
            DriverId = driver.Id,
            TruckId = truck.Id,
            RouteId = route.Id,
            Location = "Hodan Market",
            Status = "Completed",
            WeightKg = 450
        });

        await _payments.CreateAsync(new Payment
        {
            CustomerId = customer.Id!,
            Amount = 49.99m,
            Description = "Monthly waste service",
            Status = "Paid"
        });
    }
}
