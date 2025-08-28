using AutoRender.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using Yardify.Frontend.Client.Interfaces.Authentication;
using Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

namespace AutoRender.Client;

internal class WeatherForecastService(HttpClient httpClient) : IWeatherForecastService
{
    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("api/WeatherForecast") ?? [];
    }
}


