using AutoRender.Client;
using AutoRender.Client.Models;

namespace AutoRender;

internal class WeatherForecastService : IWeatherForecastService
{
    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        // Simulate asynchronous loading to demonstrate streaming rendering
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


public class AuthTokenStorage
{
    private readonly Dictionary<string, LoginResponse> _tokens = new();

    public void StoreToken(string sessionId, LoginResponse token)
    {
        _tokens[sessionId] = token;
    }

    public LoginResponse? GetToken(string sessionId)
    {
        return _tokens.TryGetValue(sessionId, out var token) ? token : null;
    }

    public void RemoveToken(string sessionId)
    {
        _tokens.Remove(sessionId);
    }
}


public class ServerAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthTokenStorage _tokenStorage;

    public ServerAuthService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, AuthTokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _tokenStorage = tokenStorage;
    }

    public async Task<bool> LoginAsync(LoginRequest loginRequest)
    {
        // Call your external API
        var response = await _httpClient.PostAsJsonAsync("https://localhost:7191/login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse != null)
            {
                var sessionId = GetOrCreateSessionId();
                _tokenStorage.StoreToken(sessionId, loginResponse);
                return true;
            }
        }
        return false;
    }

    public Task LogoutAsync()
    {
        var sessionId = GetSessionId();
        if (!string.IsNullOrEmpty(sessionId))
        {
            _tokenStorage.RemoveToken(sessionId);
        }
        return Task.CompletedTask;
    }

    public Task<UserInfo?> GetCurrentUserAsync()
    {
        var sessionId = GetSessionId();
        if (string.IsNullOrEmpty(sessionId))
            return Task.FromResult<UserInfo?>(null);

        var token = _tokenStorage.GetToken(sessionId);
        if (token == null)
            return Task.FromResult<UserInfo?>(null);

        // Parse token to get user info (simplified - you might need to decode JWT)
        return Task.FromResult<UserInfo?>(new UserInfo
        {
            Email = "user@example.com", // Extract from token
            Roles = new[] { "User" }, // Extract from token
            IsAuthenticated = true
        });
    }

    private string GetSessionId()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString("SessionId") ?? "";
    }

    private string GetOrCreateSessionId()
    {
        var context = _httpContextAccessor.HttpContext;
        var sessionId = context?.Session.GetString("SessionId");

        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context?.Session.SetString("SessionId", sessionId);
        }

        return sessionId;
    }
}
