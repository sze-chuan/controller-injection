using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Shared.Common.Clients;

namespace Shared.Common.Extensions;

public static class SharedCommonExtensions
{
    public static IServiceCollection AddSharedCommon(this IServiceCollection services)
    {
        // Register the weather API client with resilience patterns
        services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
        {
            // Using a demo weather API (in production, you'd use a real API key)
            client.BaseAddress = new Uri("https://api.weatherapi.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Retry configuration
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
            options.Retry.UseJitter = true;
            
            // Circuit breaker configuration
            options.CircuitBreaker.FailureRatio = 0.6;
            options.CircuitBreaker.MinimumThroughput = 3;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
            
            // Total request timeout
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });

        return services;
    }
}