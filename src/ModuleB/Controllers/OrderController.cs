using Microsoft.AspNetCore.Mvc;
using ModuleA.Controllers;
using ModuleB.Models;
using ModuleB.Services;

namespace ModuleB.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(IOrderService orderService, UserController userController) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAllOrders()
    {
        var orders = await orderService.GetOrdersByUserIdAsync(1);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound($"Order with ID {id} not found");
        }
        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Order>>> GetOrdersByUserId(int userId)
    {
        var orders = await orderService.GetOrdersByUserIdAsync(userId);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        try
        {
            var createdOrder = await orderService.CreateOrderAsync(order);
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
        var users = await userController.GetAllUsersDirectAsync();
        
        return Ok(new
        {
            Message = "Successfully called UserController.GetAllUsersDirectAsync() directly!",
            UsersFound = users.Count,
            Users = users
        });
    }
}