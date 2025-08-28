using AutoRender.Client;
using AutoRender.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Yardify.Frontend.Client.Interfaces.Authentication;

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
            options.IdleTimeout = TimeSpan.FromMinutes(50);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(50); // Changed from 2 hours
                options.SlidingExpiration = false; // Changed to false - don't extend
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        // Add HttpClientFactory for API calls
        builder.Services.AddHttpClient();

        // Configure services for server-side rendering
        builder.Services.AddScoped<IYardifyAuthenticationService, ServerAuthService>();
        builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        // Authorization - Use unified provider
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        // Register the unified auth state provider
        builder.Services.AddScoped<ServerAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<ServerAuthStateProvider>());

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
            app.UseWebAssemblyDebugging();
        else
            app.UseExceptionHandler("/Error");

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        // IMPORTANT: Session must come before Authentication
        app.UseSession();
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