using ModuleA.Models;

namespace ModuleA.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(User user);
}