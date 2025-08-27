using AutoRender.Client.Models;
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

public class ClientAuthService(HttpClient httpClient) : UniversalYardifyAuthenticationService(httpClient)
{
}

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IYardifyAuthenticationService _authService;
    private readonly HttpClient _httpClient;
    private AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(IYardifyAuthenticationService authService, HttpClient httpClient)
    {
        _authService = authService;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var currentUser = await _authService.GetCurrentUserInfoAsync();

            if (currentUser?.IsAuthenticated == true && !string.IsNullOrEmpty(currentUser.Email))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, currentUser.Email),
                    new Claim(ClaimTypes.Email, currentUser.Email)
                };

                if (!string.IsNullOrEmpty(currentUser.Role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, currentUser.Role));
                }

                var identity = new ClaimsIdentity(claims, "serverauth");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch { }

        return _anonymous;
    }

    public async Task<bool> LoginAsync(LoginRequestDTO loginRequest)
    {
        var success = await _authService.LoginAsync(loginRequest);
        if (success)
        {
            // Force re-evaluation of authentication state
            var authState = GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(authState);
        }
        return success;
    }

    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        // Immediately notify that user is logged out
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }
}