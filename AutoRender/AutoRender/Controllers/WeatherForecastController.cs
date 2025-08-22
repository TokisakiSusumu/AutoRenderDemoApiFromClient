using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoRender.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WeatherForecastController : ControllerBase
{
    private readonly IWeatherForecastService _weatherForecastService;

    public WeatherForecastController(IWeatherForecastService weatherForecastService)
    {
        _weatherForecastService = weatherForecastService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<WeatherForecast[]>> Get()
    {
        return await _weatherForecastService.GetWeatherForecastsAsync();
    }
}