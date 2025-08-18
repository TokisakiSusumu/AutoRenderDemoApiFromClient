using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoRender.Controllers;
[Route("api/[controller]")]
[ApiController]
public class WeatherForecastController(IWeatherForecastService weatherForecastService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WeatherForecast[]>> Get() 
    {
        return await weatherForecastService.GetWeatherForecastsAsync();
    }
}
