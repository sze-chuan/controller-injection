using Shared.Common.Clients;

namespace Worker.Services;

public class WorkerService(
    ILogger<WorkerService> logger,
    UserApiClient userApiClient,
    IWeatherApiClient weatherApiClient,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue<int>("WorkerIntervalSeconds", 30);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        logger.LogInformation("Worker Service started. Running every {Interval} seconds", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during worker execution");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Worker Service is stopping...");
                break;
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

        try
        {
            var users = await userApiClient.GetAllUsersAsync(cancellationToken);
            logger.LogInformation("Fetched {UserCount} users from Module A", users.Count);

            // Get weather data using shared weather client
            var weatherData = await weatherApiClient.GetWeatherAsync("London", cancellationToken);
            if (weatherData != null)
            {
                logger.LogInformation("Current weather in {Location}: {Description}, {Temperature}°C", 
                    weatherData.Location, weatherData.Description, weatherData.Temperature);
            }

            foreach (var user in users)
            {
                logger.LogInformation("Processing user: {UserId} - {UserName} ({UserEmail})", 
                    user.Id, user.Name, user.Email);
                    
                await ProcessUserAsync(user, cancellationToken);
            }

            logger.LogInformation("Worker completed processing all users");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while processing users");
            throw;
        }
    }

    private async Task ProcessUserAsync(ModuleA.Contracts.Models.User user, CancellationToken cancellationToken)
    {
        logger.LogDebug("Processing user business logic for user {UserId}", user.Id);
        
        await Task.Delay(100, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Worker Service is stopping.");
        await base.StopAsync(cancellationToken);
    }
}