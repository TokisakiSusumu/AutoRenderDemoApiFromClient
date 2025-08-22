using AutoRender.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace AutoRender.Client;

internal class WeatherForecastService(HttpClient httpClient) : IWeatherForecastService
{
    public async Task<WeatherForecast[]> GetWeatherForecastsAsync()
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("api/WeatherForecast");
    }
}

public class ClientAuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public ClientAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            // This calls the Blazor Server endpoint which then calls the API
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _httpClient.PostAsync("api/auth/logout", null);
        }
        catch { }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserInfo>("api/auth/current-user");
        }
        catch
        {
            return null;
        }
    }
}

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;
    private UserInfo? _currentUser;

    public CustomAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _currentUser = await _authService.GetCurrentUserAsync();

        if (_currentUser?.IsAuthenticated == true)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, _currentUser.Email),
                new Claim(ClaimTypes.Email, _currentUser.Email)
            };

            foreach (var role in _currentUser.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "serverauth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public async Task LoginAsync(LoginRequest loginRequest)
    {
        var success = await _authService.LoginAsync(loginRequest);
        if (success)
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        else
        {
            throw new InvalidOperationException("Login failed");
        }
    }

    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        _currentUser = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}


