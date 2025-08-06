using Shared.Common.Models;

namespace Shared.Common.Clients;

public interface IWeatherApiClient
{
    Task<WeatherData?> GetWeatherAsync(string city, CancellationToken cancellationToken = default);
    Task<List<WeatherData>> GetWeatherForMultipleCitiesAsync(IEnumerable<string> cities, CancellationToken cancellationToken = default);
}