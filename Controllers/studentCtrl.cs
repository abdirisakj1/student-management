using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using Tourism_Management.Models;
using Tourism_Management.Services;

namespace Tourism_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ITourismUserService _userService;

        public UsersController(ITourismUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var list = await _userService.GetAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] TourismUsers user)
        {
            if (user == null)
                return BadRequest("User payload is required.");

            var created = await _userService.CreateAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] TourismUsers updateTourismUsers)
        {
            if (updateTourismUsers == null)
                return BadRequest("User payload is required.");

            var exists = await _userService.GetByIdAsync(id);
            if (exists == null)
                return NotFound();

            updateTourismUsers._id = exists._id;
            updateTourismUsers.Id = exists.Id;

            var updated = await _userService.UpdateAsync(id, updateTourismUsers);
            return updated ? Ok(updateTourismUsers) : StatusCode(500, "Update failed.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var exists = await _userService.GetByIdAsync(id);
            if (exists == null)
                return NotFound();

            var deleted = await _userService.DeleteAsync(id);
            return deleted ? Ok(exists) : StatusCode(500, "Delete failed.");
        }
    }
}