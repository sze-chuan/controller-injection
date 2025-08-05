using Microsoft.AspNetCore.Mvc;
using ModuleA.Models;
using ModuleA.Services;

namespace ModuleA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email))
        {
            return BadRequest("Name and Email are required");
        }

        var createdUser = await _userService.CreateUserAsync(user);
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
    }

    public async Task<User?> GetUserByIdDirectAsync(int id)
    {
        return await _userService.GetUserByIdAsync(id);
    }

    public async Task<List<User>> GetAllUsersDirectAsync()
    {
        return await _userService.GetAllUsersAsync();
    }
}