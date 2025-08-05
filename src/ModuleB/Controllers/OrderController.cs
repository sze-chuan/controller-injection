using Microsoft.AspNetCore.Mvc;
using ModuleA.Controllers;
using ModuleB.Models;
using ModuleB.Services;

namespace ModuleB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly UserController _userController;

    public OrderController(IOrderService orderService, UserController userController)
    {
        _orderService = orderService;
        _userController = userController;
    }

    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAllOrders()
    {
        var orders = await _orderService.GetOrdersByUserIdAsync(1);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound($"Order with ID {id} not found");
        }
        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Order>>> GetOrdersByUserId(int userId)
    {
        var orders = await _orderService.GetOrdersByUserIdAsync(userId);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        try
        {
            var createdOrder = await _orderService.CreateOrderAsync(order);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("demo-direct-call")]
    public async Task<ActionResult> DemoDirectControllerCall()
    {
        var users = await _userController.GetAllUsersDirectAsync();
        
        return Ok(new
        {
            Message = "Successfully called UserController.GetAllUsersDirectAsync() directly!",
            UsersFound = users.Count,
            Users = users
        });
    }
}