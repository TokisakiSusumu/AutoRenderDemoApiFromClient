using BlazorApp1.Client.Services;
using BlazorApp1.Components;
using BlazorApp1.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

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
            builder.Services.AddHttpClient();

            // Register authentication services
            builder.Services.AddScoped<IAuthService, ServerAuthService>();
            builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            // Add authentication and authorization
            // Add authentication and authorization
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "BlazorAuthCookie"; // Different from API cookie
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(14);
                    options.SlidingExpiration = true;
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