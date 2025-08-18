using AutoRender.Client;
using AutoRender.Client.Pages;
using AutoRender.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AutoRender
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDistributedMemoryCache();
            // Add session for storing auth state
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add authentication services
            builder.Services.AddSingleton<AuthTokenStorage>();
            builder.Services.AddHttpClient<IAuthService, ServerAuthService>();
            builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

            // Add authorization
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<CustomAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
                provider.GetRequiredService<CustomAuthStateProvider>());
            builder.Services.AddCascadingAuthenticationState();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
                app.UseWebAssemblyDebugging();
            else
                app.UseExceptionHandler("/Error");

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession(); // Add this before authentication
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

            app.MapControllers();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();

        }
    }
}
