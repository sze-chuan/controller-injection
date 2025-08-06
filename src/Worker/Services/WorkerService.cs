namespace Worker.Services;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly UserApiClient _userApiClient;
    private readonly IConfiguration _configuration;

    public WorkerService(ILogger<WorkerService> logger, UserApiClient userApiClient, IConfiguration configuration)
    {
        _logger = logger;
        _userApiClient = userApiClient;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>("WorkerIntervalSeconds", 30);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation("Worker Service started. Running every {Interval} seconds", intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during worker execution");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Worker Service is stopping...");
                break;
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

        try
        {
            var users = await _userApiClient.GetAllUsersAsync(cancellationToken);
            _logger.LogInformation("Fetched {UserCount} users from Module A", users.Count);

            foreach (var user in users)
            {
                _logger.LogInformation("Processing user: {UserId} - {UserName} ({UserEmail})", 
                    user.Id, user.Name, user.Email);
                    
                await ProcessUserAsync(user, cancellationToken);
            }

            _logger.LogInformation("Worker completed processing all users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing users");
            throw;
        }
    }

    private async Task ProcessUserAsync(ModuleA.Contracts.Models.User user, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing user business logic for user {UserId}", user.Id);
        
        await Task.Delay(100, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker Service is stopping.");
        await base.StopAsync(cancellationToken);
    }
}