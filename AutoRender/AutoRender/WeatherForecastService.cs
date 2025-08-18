using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using System.Security.Claims;

namespace AutoRender;

internal class WeatherForecastService : IWeatherForecastService
{
    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        await Task.Delay(500);

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        }).ToArray();
        return forecasts;
    }
}
public class ServerAuthStateProvider : ServerAuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerAuthStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }
}

public class ServerAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerAuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            // Call external API for authentication
            var response = await _httpClient.PostAsJsonAsync("https://localhost:7191/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null)
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null)
                    {
                        // Store bearer token in session
                        httpContext.Session.SetString("BearerToken", loginResponse.AccessToken);
                        httpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken ?? "");

                        // Create claims for cookie authentication
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Email, loginRequest.Email),
                            new Claim(ClaimTypes.Name, loginRequest.Email)
                        };

                        // Parse roles from token if needed (simplified here)
                        claims.Add(new Claim(ClaimTypes.Role, "User"));

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                        };

                        // Sign in with cookie authentication
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
            // Log exception
            Console.WriteLine($"Login failed: {ex.Message}");
        }
        return false;
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Clear session
            httpContext.Session.Clear();

            // Sign out from cookie authentication
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    public Task<UserInfo?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            return Task.FromResult<UserInfo?>(new UserInfo
            {
                Email = email,
                Roles = roles,
                IsAuthenticated = true
            });
        }

        return Task.FromResult<UserInfo?>(null);
    }

    // Helper method to get bearer token for API calls
    public string? GetBearerToken()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString("BearerToken");
    }
}

