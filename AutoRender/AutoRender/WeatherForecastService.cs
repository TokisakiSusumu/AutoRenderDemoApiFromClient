using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

public class ServerAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public ServerAuthService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7191/");
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<bool> LoginAsync(LoginRequest loginRequest)
    {
        // This method is only called during SSR
        // For CSR, the client calls the API controller instead
        try
        {
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
                        httpContext.Session.SetString("BearerToken", loginResponse.AccessToken);
                        httpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken);

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
            var token = httpContext.Session.GetString("BearerToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                await _httpClient.PostAsync("logout", null);
            }

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

                var response = await _httpClient.GetAsync("manage/info");
                if (response.IsSuccessStatusCode)
                {
                    var identityInfo = await response.Content.ReadFromJsonAsync<IdentityUserInfo>();
                    if (identityInfo != null)
                    {
                        return new UserInfo
                        {
                            Email = identityInfo.Email,
                            Roles = [],
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

// DTOs for Identity API responses
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