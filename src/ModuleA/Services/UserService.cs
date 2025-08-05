using ModuleA.Models;

namespace ModuleA.Services;

public class UserService : IUserService
{
    private static readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow.AddDays(-30) },
        new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow.AddDays(-15) },
        new User { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", CreatedAt = DateTime.UtcNow.AddDays(-5) }
    };

    public Task<User?> GetUserByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_users.ToList());
    }

    public Task<User> CreateUserAsync(User user)
    {
        user.Id = _users.Max(u => u.Id) + 1;
        user.CreatedAt = DateTime.UtcNow;
        _users.Add(user);
        return Task.FromResult(user);
    }
}