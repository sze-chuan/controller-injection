using ModuleA.Contracts.Models;

namespace ModuleA.Contracts.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(User user);
}