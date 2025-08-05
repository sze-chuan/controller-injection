using Microsoft.Extensions.DependencyInjection;
using ModuleB.Services;

namespace ModuleB;

public static class ModuleBExtensions
{
    public static IServiceCollection AddModuleB(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        
        return services;
    }
}