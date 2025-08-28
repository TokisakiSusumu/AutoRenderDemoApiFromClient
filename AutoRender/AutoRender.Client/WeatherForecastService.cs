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

public class ClientAuthService(HttpClient httpClient) : UniversalYardifyAuthenticationService(httpClient)
{
}

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IYardifyAuthenticationService _authService;
    private AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(IYardifyAuthenticationService authService)
    {
        _authService = authService;
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
}