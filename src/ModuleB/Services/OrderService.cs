using ModuleA.Controllers;
using ModuleB.Models;
using Shared.Common.Clients;

namespace ModuleB.Services;

public class OrderService(UserController userController, IWeatherApiClient weatherApiClient) : IOrderService
{
    private static readonly List<Order> _orders = new()
    {
        new Order { Id = 1, UserId = 1, ProductName = "Laptop", Amount = 999.99m, OrderDate = DateTime.UtcNow.AddDays(-10) },
        new Order { Id = 2, UserId = 2, ProductName = "Mouse", Amount = 29.99m, OrderDate = DateTime.UtcNow.AddDays(-5) },
        new Order { Id = 3, UserId = 1, ProductName = "Keyboard", Amount = 79.99m, OrderDate = DateTime.UtcNow.AddDays(-2) }
    };

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order != null)
        {
            var user = await userController.GetUserByIdDirectAsync(order.UserId);
            if (user != null)
            {
                order.UserName = user.Name;
                order.UserEmail = user.Email;
            }

            // Add weather information using shared weather client
            try
            {
                var weather = await weatherApiClient.GetWeatherAsync("New York");
                if (weather != null)
                {
                    order.WeatherInfo = $"{weather.Description}, {weather.Temperature}°C";
                }
            }
            catch (Exception)
            {
                order.WeatherInfo = "Weather unavailable";
            }
        }
        return order;
    }

    public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
    {
        var userOrders = _orders.Where(o => o.UserId == userId).ToList();
        
        var user = await userController.GetUserByIdDirectAsync(userId);
        if (user != null)
        {
            foreach (var order in userOrders)
            {
                order.UserName = user.Name;
                order.UserEmail = user.Email;
            }
        }
        
        return userOrders;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        var user = await userController.GetUserByIdDirectAsync(order.UserId);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {order.UserId} not found");
        }

        order.Id = _orders.Any() ? _orders.Max(o => o.Id) + 1 : 1;
        order.OrderDate = DateTime.UtcNow;
        order.UserName = user.Name;
        order.UserEmail = user.Email;
        
        _orders.Add(order);
        return order;
    }
}