using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Net.Http.Headers;
using System.Security.Claims;

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

        // Configure HttpClient with API base URL
        _httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:7191/");
    }

    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        // Add bearer token to request
        var token = _httpContextAccessor.HttpContext?.Session.GetString("BearerToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        // Call your API endpoint for weather data
        var response = await _httpClient.GetAsync("api/weatherforecast");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WeatherForecast[]>() ?? [];
        }

        // Fallback to local data if API is not available
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
public class ServerAuthService : IAuthService
{
    public class IdentityLoginResponse
    {
        public string TokenType { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = "";
    }

    public class IdentityUserInfo
    {
        public string Email { get; set; } = "";
        public bool IsEmailConfirmed { get; set; }
    }
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public ServerAuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;

        // Configure HttpClient with API base URL
        _httpClient.BaseAddress = new Uri(_configuration["ApiBaseUrl"] ?? "https://localhost:7191/");
    }

    public async Task<bool> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            // Call the built-in Identity login endpoint
            var response = await _httpClient.PostAsJsonAsync("login", new
            {
                email = loginRequest.Email,
                password = loginRequest.Password
            });

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<IdentityLoginResponse>();
                if (loginResponse != null)
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null)
                    {
                        // Store tokens in session
                        httpContext.Session.SetString("BearerToken", loginResponse.AccessToken);
                        httpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken);

                        // Create cookie authentication for Blazor
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Email, loginRequest.Email),
                            new Claim(ClaimTypes.Name, loginRequest.Email)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(loginResponse.ExpiresIn)
                        };

                        await httpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed: {ex.Message}");
        }
        return false;
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Get token for logout API call
            var token = httpContext.Session.GetString("BearerToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Call Identity logout endpoint
                await _httpClient.PostAsync("logout", null);
            }

            // Clear session and cookies
            httpContext.Session.Clear();
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var token = httpContext?.Session.GetString("BearerToken");

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Call Identity manage/info endpoint
                var response = await _httpClient.GetAsync("manage/info");
                if (response.IsSuccessStatusCode)
                {
                    var identityInfo = await response.Content.ReadFromJsonAsync<IdentityUserInfo>();
                    if (identityInfo != null)
                    {
                        return new UserInfo
                        {
                            Email = identityInfo.Email,
                            Roles = [], // Identity doesn't return roles in info endpoint by default
                            IsAuthenticated = true
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get current user failed: {ex.Message}");
        }

        return null;
    }
}

