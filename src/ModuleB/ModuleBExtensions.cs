using Microsoft.Extensions.DependencyInjection;
using ModuleB.Services;
using Shared.Common.Extensions;

namespace ModuleB;

public static class ModuleBExtensions
{
    public static IServiceCollection AddModuleB(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddSharedCommon();
        
        return services;
    }
}