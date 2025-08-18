using AutoRender.Client.Models;
using System.Net.Http.Json;

namespace AutoRender.Client;

internal class WeatherForecastService(HttpClient httpClient) : IWeatherForecastService
{
    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("api/WeatherForecast");
    }
}
