using AutoRender.Client;
using AutoRender.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace AutoRender;

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

        // Session for storing bearer tokens
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(2);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax; // Add this
        });

        // Cookie authentication for the Blazor app
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.SlidingExpiration = true;
                options.Cookie.SameSite = SameSiteMode.Lax; // Add this
            });

        // Add HttpClientFactory for API calls
        builder.Services.AddHttpClient();

        // Configure services for server-side rendering
        builder.Services.AddScoped<IAuthService, ServerAuthService>();
        builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        // Authorization
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthStateProvider>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
            app.UseWebAssemblyDebugging();
        else
            app.UseExceptionHandler("/Error");

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting(); // Add this
        app.UseSession(); // Must be before UseEndpoints
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