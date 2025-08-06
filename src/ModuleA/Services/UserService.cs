using Microsoft.AspNetCore.Http;
using ModuleA.Models;

namespace ModuleA.Services;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow.AddDays(-30) },
        new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow.AddDays(-15) },
        new User { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", CreatedAt = DateTime.UtcNow.AddDays(-5) }
    };

    public UserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ModuleA.Contracts.Models.User?> GetUserByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && user != null)
        {
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            Console.WriteLine($"UserService: Getting user {id}, User-Agent: {userAgent}");
        }
        
        return Task.FromResult<ModuleA.Contracts.Models.User?>(user);
    }

    public Task<List<ModuleA.Contracts.Models.User>> GetAllUsersAsync()
    {
        return Task.FromResult(_users.Cast<ModuleA.Contracts.Models.User>().ToList());
    }

    public Task<ModuleA.Contracts.Models.User> CreateUserAsync(ModuleA.Contracts.Models.User user)
    {
        var localUser = new User { Name = user.Name, Email = user.Email };
        localUser.Id = _users.Max(u => u.Id) + 1;
        localUser.CreatedAt = DateTime.UtcNow;
        _users.Add(localUser);
        return Task.FromResult<ModuleA.Contracts.Models.User>(localUser);
    }
}