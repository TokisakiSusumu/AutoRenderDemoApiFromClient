using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoRender.Controllers;
[Route("api/[controller]")]
[ApiController]
public class WeatherForecastController(IWeatherForecastService weatherForecastService) : ControllerBase
{
    [HttpGet]
    [Authorize] // Add this to protect the endpoint
    public async Task<ActionResult<WeatherForecast[]>> Get()
    {
        return await weatherForecastService.GetWeatherForecastsAsync();
    }
}


[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var success = await _authService.LoginAsync(loginRequest);
        if (success)
            return Ok(new { success = true });
        return Unauthorized(new { success = false, message = "Invalid credentials" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok();
    }

    [HttpGet("current-user")]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return Ok(new UserInfo { IsAuthenticated = false });
        return Ok(user);
    }
}
