using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWasteManagement.Models;
using SmartWasteManagement.Services;

namespace SmartWasteManagement.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = Roles.Admin)]
public class CustomersController : ControllerBase
{
    private readonly IUserService _users;

    public CustomersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummary>>> GetCustomers()
    {
        var customers = await _users.GetByRoleAsync(Roles.Customer);
        return Ok(customers.Select(UserSummary.FromUser));
    }

    [HttpPost]
    public async Task<ActionResult<UserSummary>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.GetByEmailAsync(email) is not null)
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            FullName = request.FullName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Roles.Customer,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = true,
        };

        await _users.CreateAsync(user);
        return CreatedAtAction(nameof(GetCustomers), UserSummary.FromUser(user));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(string id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user is null || !Roles.IsCustomer(user.Role))
            return NotFound();
        var ok = await _users.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}

public class CreateCustomerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
