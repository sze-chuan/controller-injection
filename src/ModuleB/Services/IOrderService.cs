using ModuleB.Models;

namespace ModuleB.Services;

public interface IOrderService
{
    Task<Order?> GetOrderByIdAsync(int id);
    Task<List<Order>> GetOrdersByUserIdAsync(int userId);
    Task<Order> CreateOrderAsync(Order order);
}