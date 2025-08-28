using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Yardify.Frontend.Client.Interfaces.Authentication;

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

            builder.Services.AddScoped<IYardifyAuthenticationService, ClientAuthService>();
            builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

            builder.Services.AddAuthorizationCore();

            // Use the improved auth state provider
            builder.Services.AddScoped<ClientAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<ClientAuthStateProvider>());

            await builder.Build().RunAsync();
        }
    }
}