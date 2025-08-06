using System.Text.Json;
using ModuleA.Contracts.Models;

namespace Worker.Services;

public class UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching user with ID: {UserId}", id);
            
            var response = await httpClient.GetAsync($"/api/user/{id}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning("User with ID {UserId} not found", id);
                    return null;
                }
                
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var user = JsonSerializer.Deserialize<User>(json, _jsonOptions);
            
            logger.LogInformation("Successfully fetched user: {UserName} ({UserEmail})", user?.Name, user?.Email);
            return user;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error occurred while fetching user with ID: {UserId}", id);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout while fetching user with ID: {UserId}", id);
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON deserialization error while fetching user with ID: {UserId}", id);
            throw;
        }
    }

    public async Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching all users");
            
            var response = await httpClient.GetAsync("/api/user", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
            
            logger.LogInformation("Successfully fetched {UserCount} users", users.Count);
            return users;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error occurred while fetching all users");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout while fetching all users");
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON deserialization error while fetching all users");
            throw;
        }
    }
}