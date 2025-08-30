using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorApp1.Client.Services;

namespace BlazorApp1.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Configure HttpClient to use the server's base address
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });

            // Register authentication services
            builder.Services.AddScoped<IAuthService, ClientAuthService>();
            builder.Services.AddScoped<AuthenticationStateProvider, ClientAuthenticationStateProvider>();

            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();

            await builder.Build().RunAsync();
        }
    }
}