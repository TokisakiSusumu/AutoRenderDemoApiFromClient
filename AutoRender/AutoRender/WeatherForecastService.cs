using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Yardify.Frontend.Client.Interfaces.Authentication;
using Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

namespace AutoRender;

internal class WeatherForecastService : IWeatherForecastService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public WeatherForecastService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;

        _httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:7191/");
    }

    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // NEW: Check if token is expired
        var expirationStr = httpContext?.Session.GetString("TokenExpiration");
        if (!string.IsNullOrEmpty(expirationStr))
        {
            if (DateTimeOffset.TryParse(expirationStr, out var expiration))
            {
                if (expiration <= DateTimeOffset.UtcNow)
                {
                    // Token expired - force logout
                    httpContext.Session.Clear();
                    throw new InvalidOperationException("Session expired. Please login again.");
                }
            }
        }

        var token = httpContext?.Session.GetString("BearerToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var response = await _httpClient.GetAsync("api/weatherforecast");

            // NEW: Better handling of 401
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token is invalid - clear session and throw
                httpContext?.Session.Clear();
                throw new InvalidOperationException("Session expired. Please login again.");
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WeatherForecast[]>() ?? [];
            }
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw session expired exceptions
        }
        catch { }

        // Fallback to local data
        await Task.Delay(500);
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        }).ToArray();
    }
}

public class ServerAuthService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) :
    UniversalYardifyAuthenticationService(CreateHttpClient(httpClientFactory, httpContextAccessor))
{
    private static HttpClient CreateHttpClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        var httpClient = httpClientFactory.CreateClient();
        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            httpClient.BaseAddress = new Uri(
                $"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
        }
        return httpClient;
    }
}
