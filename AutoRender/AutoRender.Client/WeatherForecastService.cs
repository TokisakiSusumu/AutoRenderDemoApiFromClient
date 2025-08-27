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
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("api/WeatherForecast");
    }
}

public class ClientAuthService(HttpClient httpClient) : UniversalYardifyAuthenticationService(httpClient)
{
}

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IYardifyAuthenticationService _authService;
    private UserDetailDTO? _currentUser;

    public CustomAuthStateProvider(IYardifyAuthenticationService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        UserDetailDTO? _currentUser = await _authService.GetCurrentUserInfoAsync();

        if (_currentUser?.IsAuthenticated == true)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, _currentUser.Email),
                new Claim(ClaimTypes.Email, _currentUser.Email),
                new Claim(ClaimTypes.Role, _currentUser.Role)
            };

            var identity = new ClaimsIdentity(claims, "serverauth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    //public async Task LoginAsync(LoginRequest loginRequest)
    //{
    //    var success = await _authService.LoginAsync(loginRequest);
    //    if (success)
    //    {
    //        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    //    }
    //    else
    //    {
    //        throw new InvalidOperationException("Login failed");
    //    }
    //}

    //public async Task LogoutAsync()
    //{
    //    await _authService.LogoutAsync();
    //    _currentUser = null;
    //    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    //}
}


