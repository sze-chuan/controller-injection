using Worker.Services;
using Shared.Common.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<WorkerService>();

// Add shared common services (includes weather API client)
builder.Services.AddSharedCommon();

builder.Services.AddHttpClient<UserApiClient>(client =>
{
    var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl", "https://localhost:7000");
    client.BaseAddress = new Uri(apiBaseUrl!);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.Retry.MaxDelay = TimeSpan.FromSeconds(30);
    options.Retry.UseJitter = true;
    
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
    
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
});

var host = builder.Build();
host.Run();