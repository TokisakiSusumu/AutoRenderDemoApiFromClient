using BlazorApp1.Client.Services;
using BlazorApp1.Components;
using BlazorApp1.Middleware;
using BlazorApp1.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;

namespace BlazorApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            // Add HttpContextAccessor for cookie handling
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddHttpClient();
            // Add HttpClient factory
            // Remove the old AddHttpClient registration and replace with:
            builder.Services.AddScoped<WebApiHttpClient>();

            // Register authentication services
            builder.Services.AddScoped<IAuthService, ServerAuthService>();
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            // Add authentication and authorization
            // Add authentication and authorization
            // Replace the authentication configuration with:
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "BlazorAuthCookie";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";

                    // Blazor cookie expires in 10 seconds
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    options.SlidingExpiration = true;

                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILogger<Program>>();

                            // Check if the API cookie still exists
                            var hasApiCookie = context.HttpContext.Request.Cookies
                                .ContainsKey(".AspNetCore.Identity.Application");

                            if (!hasApiCookie)
                            {
                                logger.LogWarning(
                                    "Blazor session valid but API cookie missing - forcing re-login");

                                // The API session is gone, so invalidate Blazor session too
                                context.RejectPrincipal();
                            }
                            else
                            {
                                var sessionId = context.Principal?.FindFirst("SessionId")?.Value;
                                logger.LogDebug(
                                    "Session {SessionId} validated - both cookies present",
                                    sessionId);
                            }
                        }
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            // Add controllers for API proxy
            builder.Services.AddControllers();
            builder.Services.AddAntiforgery();
            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseAuthentication();
            app.UseMiddleware<AuthenticationSyncMiddleware>();
            app.UseAuthorization();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.MapControllers();

            app.Run();
        }
    }
}