namespace Shared.Common.Models;

public class WeatherData
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public DateTime Timestamp { get; set; }
}

public class WeatherApiResponse
{
    public WeatherMain Main { get; set; } = new();
    public List<WeatherDescription> Weather { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public long Dt { get; set; }
}

public class WeatherMain
{
    public double Temp { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
}

public class WeatherDescription
{
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}