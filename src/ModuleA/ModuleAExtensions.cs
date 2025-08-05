using Microsoft.Extensions.DependencyInjection;
using ModuleA.Controllers;
using ModuleA.Services;

namespace ModuleA;

public static class ModuleAExtensions
{
    public static IServiceCollection AddModuleA(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<UserController>();
        
        return services;
    }
}