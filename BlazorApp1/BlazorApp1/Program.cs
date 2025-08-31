using BlazorApp1.Client.Services;
using BlazorApp1.Components;
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

            // Add HttpClient factory
            builder.Services.AddHttpClient("WebApi", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7191/");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = false
            });
            // Register authentication services
            builder.Services.AddScoped<IAuthService, ServerAuthService>();
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            // Add authentication and authorization
            // Add authentication and authorization
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "BlazorAuthCookie";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";

                options.ExpireTimeSpan = TimeSpan.FromSeconds(10); // Was 10 seconds
                options.SlidingExpiration = true;
                // This is the important part for inactivity timeout
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var lastActivity = context.Properties.GetString("LastActivity");

                        if (!string.IsNullOrEmpty(lastActivity))
                        {
                            var lastActivityTime = DateTimeOffset.Parse(lastActivity);
                            var timeSinceActivity = DateTimeOffset.UtcNow - lastActivityTime;

                            logger.LogInformation("Cookie validation: Last activity {LastActivity}, Time since: {TimeSince}s",
                                lastActivityTime, timeSinceActivity.TotalSeconds);

                            if (timeSinceActivity > TimeSpan.FromSeconds(10))
                            {
                                logger.LogWarning("Cookie expired due to inactivity. Signing out user.");
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                            else
                            {
                                logger.LogInformation("Cookie renewed. Updating last activity time.");
                                context.Properties.SetString("LastActivity", DateTimeOffset.UtcNow.ToString("o"));
                                context.ShouldRenew = true;
                            }
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