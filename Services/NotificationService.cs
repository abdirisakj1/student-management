using MongoDB.Driver;

using SmartWasteManagement.Data;

using SmartWasteManagement.Models;

using SmartWasteManagement.Hubs;

using Microsoft.AspNetCore.SignalR;

using Microsoft.Extensions.Options;



namespace SmartWasteManagement.Services;



public interface INotificationService

{

    Task<List<Notification>> GetByUserIdAsync(string userId);

    Task<List<Notification>> GetUnreadByUserIdAsync(string userId);

    Task<Notification> SendAsync(

        string userId,

        string title,

        string message,

        string type = "Info",

        string? actionUrl = null,

        string? actionText = null,

        string? secondaryActionUrl = null,

        string? secondaryActionText = null,

        string? referenceId = null,

        string? referenceType = null);



    Task SendToRoleAsync(

        string role,

        string title,

        string message,

        string type = "Info",

        string? actionUrl = null,

        string? actionText = null,

        string? secondaryActionUrl = null,

        string? secondaryActionText = null,

        string? referenceId = null,

        string? referenceType = null);



    Task SendToEligibleDriversAsync(

        string title,

        string message,

        string type = "Info",

        string? actionUrl = null,

        string? actionText = null,

        string? referenceId = null,

        string? referenceType = null);



    Task CompletePrimaryByReferenceAsync(string referenceType, string referenceId, string completedText, string? role = null, string? userId = null);

    Task HidePrimaryByReferenceAsync(string referenceType, string referenceId, string userId);

    Task CompleteCustomerPaymentNotificationAsync(string userId, string paymentId, string completedText = "Paid");
    Task CompleteAdminActionByTextAsync(string role, string previousActionText, string completedText, string? referenceId = null);

    Task<bool> MarkReadAsync(string id, string userId);

    Task<bool> MarkAllReadAsync(string userId);

    Task<bool> DeleteAsync(string id);

}



public class NotificationService : INotificationService

{

    private readonly IMongoCollection<Notification> _notifications;

    private readonly IUserService _users;

    private readonly ITruckService _trucks;

    private readonly IHubContext<LiveTrackingHub> _hub;



    public NotificationService(

        IMongoDatabase database,

        IOptions<MongoDbSettings> options,

        IUserService users,

        ITruckService trucks,

        IHubContext<LiveTrackingHub> hub)

    {

        _notifications = database.GetCollection<Notification>(options.Value.NotificationsCollectionName);

        _users = users;

        _trucks = trucks;

        _hub = hub;

    }



    public async Task<List<Notification>> GetByUserIdAsync(string userId) =>

        await _notifications.Find(n => n.UserId == userId)

            .SortByDescending(n => n.CreatedAt).ToListAsync();



    public async Task<List<Notification>> GetUnreadByUserIdAsync(string userId) =>

        await _notifications.Find(n => n.UserId == userId && !n.ReadStatus)

            .SortByDescending(n => n.CreatedAt).ToListAsync();



    public async Task<Notification> SendAsync(

        string userId,

        string title,

        string message,

        string type = "Info",

        string? actionUrl = null,

        string? actionText = null,

        string? secondaryActionUrl = null,

        string? secondaryActionText = null,

        string? referenceId = null,

        string? referenceType = null)

    {

        var notification = new Notification

        {

            UserId = userId,

            Title = title,

            Message = message,

            Type = type,

            ActionUrl = actionUrl,

            ActionText = actionText,

            SecondaryActionUrl = secondaryActionUrl,

            SecondaryActionText = secondaryActionText,

            ReferenceId = referenceId,

            ReferenceType = referenceType,

            ReadStatus = false,

            CreatedAt = DateTime.UtcNow

        };

        await _notifications.InsertOneAsync(notification);

        await PushToUserAsync(notification);

        return notification;

    }



    public async Task SendToRoleAsync(

        string role,

        string title,

        string message,

        string type = "Info",

        string? actionUrl = null,

        string? actionText = null,

        string? secondaryActionUrl = null,

        string? secondaryActionText = null,

        string? referenceId = null,

        string? referenceType = null)

    {

        var users = await _users.GetByRoleAsync(role);

        foreach (var user in users.Where(u => !string.IsNullOrEmpty(u.Id)))

            await SendAsync(user.Id!, title, message, type, actionUrl, actionText, secondaryActionUrl, secondaryActionText, referenceId, referenceType);

    }



    public async Task SendToEligibleDriversAsync(

        string title,

        string message,

        string type = "Info",

        string? actionUrl = null,

        string? actionText = null,

        string? referenceId = null,

        string? referenceType = null)

    {

        var drivers = await _users.GetByRoleAsync(Roles.Driver);

        foreach (var driver in drivers.Where(d => !string.IsNullOrEmpty(d.Id)))

        {

            var truck = await _trucks.GetByDriverIdAsync(driver.Id!);

            if (truck is null && !string.IsNullOrEmpty(driver.AssignedTruckId))

                truck = await _trucks.GetByIdAsync(driver.AssignedTruckId);



            if (truck is null || !string.Equals(truck.Status, "Active", StringComparison.OrdinalIgnoreCase))

                continue;

            var isOnline = string.Equals(driver.Status, "Online", StringComparison.OrdinalIgnoreCase)

                || string.Equals(driver.Status, "Active", StringComparison.OrdinalIgnoreCase);

            if (!isOnline)

                continue;

            await SendAsync(driver.Id!, title, message, type, actionUrl, actionText, null, null, referenceId, referenceType);

        }

    }



    public async Task CompletePrimaryByReferenceAsync(

        string referenceType,

        string referenceId,

        string completedText,

        string? role = null,

        string? userId = null)

    {

        var filter = Builders<Notification>.Filter.And(

            Builders<Notification>.Filter.Eq(n => n.ReferenceType, referenceType),

            Builders<Notification>.Filter.Eq(n => n.ReferenceId, referenceId));



        if (!string.IsNullOrEmpty(userId))

            filter = Builders<Notification>.Filter.And(filter, Builders<Notification>.Filter.Eq(n => n.UserId, userId));

        else if (!string.IsNullOrEmpty(role))

        {

            var users = await _users.GetByRoleAsync(role);

            var ids = users.Select(u => u.Id).Where(id => !string.IsNullOrEmpty(id)).ToList();

            filter = Builders<Notification>.Filter.And(filter, Builders<Notification>.Filter.In(n => n.UserId, ids));

        }



        var update = Builders<Notification>.Update

            .Set(n => n.PrimaryActionDisabled, true)

            .Set(n => n.ActionText, completedText)

            .Set(n => n.HideSecondaryAction, true);



        var result = await _notifications.UpdateManyAsync(filter, update);

        if (result.ModifiedCount > 0)

            await PushNotificationsMatchingFilterAsync(filter);



        if (!string.IsNullOrEmpty(role))

        {

            var previousText = completedText switch

            {

                "Charged" => "Charge request",

                "Confirmed" => "Check payment",

                _ => string.Empty

            };

            if (!string.IsNullOrEmpty(previousText))

                await CompleteAdminActionByTextAsync(role, previousText, completedText, referenceId);

        }

    }



    public async Task CompleteAdminActionByTextAsync(string role, string previousActionText, string completedText, string? referenceId = null)

    {

        var users = await _users.GetByRoleAsync(role);

        var ids = users.Select(u => u.Id).Where(id => !string.IsNullOrEmpty(id)).ToList();

        if (ids.Count == 0)

            return;



        var filters = new List<FilterDefinition<Notification>>

        {

            Builders<Notification>.Filter.In(n => n.UserId, ids),

            Builders<Notification>.Filter.Eq(n => n.ActionText, previousActionText),

            Builders<Notification>.Filter.Eq(n => n.PrimaryActionDisabled, false)

        };



        if (!string.IsNullOrEmpty(referenceId))

            filters.Add(Builders<Notification>.Filter.Eq(n => n.ReferenceId, referenceId));



        var filter = Builders<Notification>.Filter.And(filters);

        var update = Builders<Notification>.Update

            .Set(n => n.PrimaryActionDisabled, true)

            .Set(n => n.ActionText, completedText)

            .Set(n => n.HideSecondaryAction, true);



        var result = await _notifications.UpdateManyAsync(filter, update);

        if (result.ModifiedCount > 0)

            await PushNotificationsMatchingFilterAsync(filter);

    }



    public async Task HidePrimaryByReferenceAsync(string referenceType, string referenceId, string userId)

    {

        var filter = Builders<Notification>.Filter.And(

            Builders<Notification>.Filter.Eq(n => n.ReferenceType, referenceType),

            Builders<Notification>.Filter.Eq(n => n.ReferenceId, referenceId),

            Builders<Notification>.Filter.Eq(n => n.UserId, userId));



        var update = Builders<Notification>.Update.Set(n => n.HidePrimaryAction, true);

        await _notifications.UpdateManyAsync(filter, update);

    }



    public async Task CompleteCustomerPaymentNotificationAsync(string userId, string paymentId, string completedText = "Paid")

    {

        var byRef = Builders<Notification>.Filter.And(

            Builders<Notification>.Filter.Eq(n => n.UserId, userId),

            Builders<Notification>.Filter.Eq(n => n.ReferenceId, paymentId));



        var byText = Builders<Notification>.Filter.And(

            Builders<Notification>.Filter.Eq(n => n.UserId, userId),

            Builders<Notification>.Filter.In(n => n.ActionText, new[] { "Complete payment", "Complete Payment" }));



        var filter = Builders<Notification>.Filter.Or(byRef, byText);



        var update = Builders<Notification>.Update

            .Set(n => n.PrimaryActionDisabled, true)

            .Set(n => n.ActionText, completedText)

            .Set(n => n.HideSecondaryAction, true);



        var result = await _notifications.UpdateManyAsync(filter, update);

        if (result.ModifiedCount > 0)

            await PushNotificationsMatchingFilterAsync(filter);

    }



    private async Task PushNotificationsMatchingFilterAsync(FilterDefinition<Notification> filter)

    {

        var list = await _notifications.Find(filter).ToListAsync();

        foreach (var n in list)

            await PushToUserAsync(n);

    }



    public async Task<bool> MarkReadAsync(string id, string userId)

    {

        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))

            return false;

        var update = Builders<Notification>.Update.Set(n => n.ReadStatus, true);

        var result = await _notifications.UpdateOneAsync(

            n => n.Id == id && n.UserId == userId, update);

        return result.ModifiedCount > 0;

    }



    public async Task<bool> MarkAllReadAsync(string userId)

    {

        var update = Builders<Notification>.Update.Set(n => n.ReadStatus, true);

        var result = await _notifications.UpdateManyAsync(

            n => n.UserId == userId && !n.ReadStatus, update);

        return result.ModifiedCount > 0;

    }



    public async Task<bool> DeleteAsync(string id)

    {

        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))

            return false;

        var result = await _notifications.DeleteOneAsync(n => n.Id == id);

        return result.DeletedCount > 0;

    }



    private async Task PushToUserAsync(Notification notification)

    {

        if (!string.IsNullOrWhiteSpace(notification.UserId))

            await _hub.Clients.User(notification.UserId).SendAsync("NotificationReceived", notification);

    }

}


