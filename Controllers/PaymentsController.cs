using System.Security.Claims;

using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using SmartWasteManagement.Models;

using SmartWasteManagement.Services;



namespace SmartWasteManagement.Controllers;



[ApiController]

[Route("api/payments")]

[Authorize]

public class PaymentsController : ControllerBase

{

    private readonly IPaymentService _payments;

    private readonly INotificationService _notifications;

    private readonly IUserService _users;

    private readonly IPickupRequestService _pickups;



    public PaymentsController(

        IPaymentService payments,

        INotificationService notifications,

        IUserService users,

        IPickupRequestService pickups)

    {

        _payments = payments;

        _notifications = notifications;

        _users = users;

        _pickups = pickups;

    }



    [HttpGet]

    public async Task<ActionResult<IEnumerable<Payment>>> GetAll()

    {

        if (User.IsInRole(Roles.Customer))

        {

            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            return Ok(await _payments.GetByCustomerIdAsync(customerId!));

        }

        return Ok(await _payments.GetAllAsync());

    }



    [HttpGet("pending")]

    [Authorize(Roles = Roles.Admin)]

    public async Task<ActionResult<IEnumerable<Payment>>> GetPending() =>

        Ok(await _payments.GetPendingAsync());



    [HttpPost("charge")]

    [Authorize(Roles = Roles.Admin)]

    public async Task<ActionResult<Payment>> AdminCharge([FromBody] AdminChargePayload payload)

    {

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        var customer = await _users.GetByIdAsync(payload.CustomerId);

        if (customer is null)

            return NotFound(new { message = "Customer not found." });



        var customerPickups = await _pickups.GetByCustomerIdAsync(payload.CustomerId);

        var pendingPickup = customerPickups.FirstOrDefault(p => p.Status == "Pending");

        if (pendingPickup is null)

            return BadRequest(new { message = "Customer has no active pickup request." });



        var pickupId = payload.PickupRequestId ?? pendingPickup.Id;



        var payment = await _payments.AdminChargeAsync(

            payload.CustomerId, adminId!, payload.Amount, payload.Description, pickupId);



        pendingPickup.Status = "AwaitingPayment";

        await _pickups.UpdateAsync(pickupId!, pendingPickup);



        await _notifications.CompletePrimaryByReferenceAsync(

            "PickupRequest", pickupId!, "Charged", Roles.Admin);



        await _notifications.SendAsync(

            payload.CustomerId,

            "Payment required",

            $"You need to pay ${payment.Amount:F2} to continue the process.",

            "Payment",

            actionUrl: "/customer/payments",

            actionText: "Complete payment",

            secondaryActionUrl: "/customer/payments",

            secondaryActionText: "Cancel",

            referenceId: payment.Id,

            referenceType: "Payment");



        return CreatedAtAction(nameof(GetAll), payment);

    }



    [HttpPut("{id}/pay")]

    [Authorize(Roles = Roles.Customer)]

    public async Task<ActionResult<Payment>> CustomerPay(string id)

    {

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        var payment = await _payments.GetByIdAsync(id);

        if (payment is null) return NotFound();



        var ok = await _payments.CustomerPayAsync(id, customerId!);

        if (!ok) return BadRequest(new { message = "Payment cannot be processed." });



        await _notifications.CompleteCustomerPaymentNotificationAsync(customerId!, id);



        var customer = await _users.GetByIdAsync(customerId!);

        var name = customer?.FullName ?? "Customer";

        await _notifications.SendToRoleAsync(

            Roles.Admin,

            "Payment received",

            $"Customer {name} paid ${payment.Amount:F2}",

            "Payment",

            actionUrl: "/admin/payments",

            actionText: "Check payment",

            referenceId: id,

            referenceType: "PaymentApproval");



        await NotifyDriversForPaidPickupAsync(payment, name);



        var updated = await _payments.GetByIdAsync(id);

        return Ok(updated);

    }



    [HttpPut("{id}/customer-decline")]

    [Authorize(Roles = Roles.Customer)]

    public async Task<ActionResult<Payment>> CustomerDecline(string id)

    {

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        var ok = await _payments.CustomerDeclineAsync(id, customerId!);

        if (!ok) return BadRequest();

        var updated = await _payments.GetByIdAsync(id);

        return Ok(updated);

    }



    [HttpPut("{id}/approve")]

    [Authorize(Roles = Roles.Admin)]

    public async Task<ActionResult<Payment>> ApprovePayment(string id, [FromBody] ApprovalPayload? payload)

    {

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        var payment = await _payments.GetByIdAsync(id);

        if (payment is null) return NotFound();



        var ok = await _payments.AdminApproveAsync(id, adminId!, payload?.Notes);

        if (!ok) return BadRequest();



        await _notifications.CompletePrimaryByReferenceAsync(

            "PaymentApproval", id, "Confirmed", Roles.Admin);



        await _notifications.CompleteCustomerPaymentNotificationAsync(

            payment.CustomerId, id, "Completed");



        var pickupId = payment.PickupRequestId;

        if (string.IsNullOrEmpty(pickupId))

        {

            var customerPickups = await _pickups.GetByCustomerIdAsync(payment.CustomerId);

            pickupId = customerPickups

                .FirstOrDefault(p => p.Status is "AwaitingPayment" or "Pending" or "Approved")?.Id;

        }



        var customer = await _users.GetByIdAsync(payment.CustomerId);

        var customerName = customer?.FullName ?? "Customer";

        await NotifyDriversForPaidPickupAsync(payment, customerName);



        var updated = await _payments.GetByIdAsync(id);

        return Ok(updated);

    }



    private async Task NotifyDriversForPaidPickupAsync(Payment payment, string customerName)

    {

        var pickupId = payment.PickupRequestId;

        if (string.IsNullOrEmpty(pickupId))

        {

            var customerPickups = await _pickups.GetByCustomerIdAsync(payment.CustomerId);

            pickupId = customerPickups

                .FirstOrDefault(p => p.Status is "AwaitingPayment" or "Pending" or "Approved")?.Id;

        }



        if (string.IsNullOrEmpty(pickupId))

            return;



        var pickup = await _pickups.GetByIdAsync(pickupId);

        if (pickup is null)

            return;



        var wasAlreadyApproved = string.Equals(pickup.Status, "Approved", StringComparison.OrdinalIgnoreCase);

        if (!wasAlreadyApproved)

        {

            pickup.Status = "Approved";

            await _pickups.UpdateAsync(pickupId, pickup);

        }



        if (wasAlreadyApproved)

            return;



        await _notifications.SendToEligibleDriversAsync(

            "Pickup request ready",

            $"Pickup request for customer {customerName} is ready.",

            "Pickup",

            actionUrl: "/driver/tasks",

            actionText: "View task",

            referenceId: pickupId,

            referenceType: "PickupTask");

    }



    [HttpPut("{id}/decline")]

    [Authorize(Roles = Roles.Admin)]

    public async Task<ActionResult<Payment>> DeclinePayment(string id, [FromBody] ApprovalPayload? payload)

    {

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        var ok = await _payments.AdminDeclineAsync(id, adminId!, payload?.Notes);

        if (!ok) return NotFound();

        var updated = await _payments.GetByIdAsync(id);

        return Ok(updated);

    }

}



public class AdminChargePayload

{

    [Required]

    public string CustomerId { get; set; } = string.Empty;



    [Range(0.01, double.MaxValue)]

    public decimal Amount { get; set; }



    public string? Description { get; set; }



    public string? PickupRequestId { get; set; }

}



public class ApprovalPayload

{

    public string? Notes { get; set; }

}


