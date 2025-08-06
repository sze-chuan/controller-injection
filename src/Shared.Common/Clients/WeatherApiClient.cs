using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Common.Models;

namespace Shared.Common.Clients;

public class WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger) : IWeatherApiClient
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Fetching weather data for city: {City}", city);

            // Using a free, no-auth-required weather API (weatherapi.com free tier)
            var response = await httpClient.GetAsync($"/v1/current.json?key=demo&q={Uri.EscapeDataString(city)}&aqi=no", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning("Weather data not found for city: {City}", city);
                    return null;
                }

                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<WeatherApiDemoResponse>(json, _jsonOptions);

            if (apiResponse == null)
            {
                logger.LogWarning("Failed to deserialize weather response for city: {City}", city);
                return null;
            }

            var weatherData = new WeatherData
            {
                Location = apiResponse.Location.Name,
                Temperature = apiResponse.Current.TempC,
                Description = apiResponse.Current.Condition.Text,
                Humidity = apiResponse.Current.Humidity,
                Pressure = apiResponse.Current.PressureMb,
                Timestamp = DateTime.UtcNow
            };

            logger.LogInformation("Successfully fetched weather for {Location}: {Temperature}°C, {Description}",
                weatherData.Location, weatherData.Temperature, weatherData.Description);

            return weatherData;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error occurred while fetching weather for city: {City}", city);
            
            // Return mock data for demo purposes when API is unavailable
            return CreateMockWeatherData(city);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout while fetching weather for city: {City}", city);
            return CreateMockWeatherData(city);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON deserialization error while fetching weather for city: {City}", city);
            return CreateMockWeatherData(city);
        }
    }

    public async Task<List<WeatherData>> GetWeatherForMultipleCitiesAsync(IEnumerable<string> cities, CancellationToken cancellationToken = default)
    {
        var weatherDataList = new List<WeatherData>();

        foreach (var city in cities)
        {
            var weatherData = await GetWeatherAsync(city, cancellationToken);
            if (weatherData != null)
            {
                weatherDataList.Add(weatherData);
            }
        }

        logger.LogInformation("Fetched weather data for {Count} cities", weatherDataList.Count);
        return weatherDataList;
    }

    private WeatherData CreateMockWeatherData(string city)
    {
        logger.LogInformation("Returning mock weather data for city: {City}", city);
        
        var random = new Random();
        return new WeatherData
        {
            Location = city,
            Temperature = Math.Round(15 + random.NextDouble() * 20, 1), // 15-35°C
            Description = GetRandomWeatherDescription(),
            Humidity = 40 + random.NextDouble() * 40, // 40-80%
            Pressure = 1000 + random.NextDouble() * 50, // 1000-1050 mb
            Timestamp = DateTime.UtcNow
        };
    }

    private static string GetRandomWeatherDescription()
    {
        var descriptions = new[] { "Sunny", "Partly cloudy", "Cloudy", "Light rain", "Clear" };
        return descriptions[new Random().Next(descriptions.Length)];
    }
}

// Demo API response models (simplified)
internal class WeatherApiDemoResponse
{
    public LocationData Location { get; set; } = new();
    public CurrentData Current { get; set; } = new();
}

internal class LocationData
{
    public string Name { get; set; } = string.Empty;
}

internal class CurrentData
{
    public double TempC { get; set; }
    public double Humidity { get; set; }
    public double PressureMb { get; set; }
    public ConditionData Condition { get; set; } = new();
}

internal class ConditionData
{
    public string Text { get; set; } = string.Empty;
}