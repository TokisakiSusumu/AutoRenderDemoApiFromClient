using AutoRender.Client.Models;

namespace AutoRender.Client;

public interface IWeatherForecastService
{
    Task<WeatherForecast[]> GetWeatherForecastsAsync();
}
