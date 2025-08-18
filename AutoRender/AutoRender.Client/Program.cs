using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace AutoRender.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddScoped(sp => new HttpClient 
            { 
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
            });
            builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();
            await builder.Build().RunAsync();
        }
    }
}
