using AutoRender.Client.Models;

namespace AutoRender.Client;

public interface IWeatherForecastService
{
    Task<WeatherForecast[]> GetWeatherForecastsAsync();
}

public interface IAuthService
{
    Task<bool> LoginAsync(LoginRequest loginRequest);
    Task LogoutAsync();
    Task<UserInfo?> GetCurrentUserAsync();
}