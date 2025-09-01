using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BlazorApp1.Middleware;

public class AuthenticationSyncMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationSyncMiddleware> _logger;

    public AuthenticationSyncMiddleware(RequestDelegate next, ILogger<AuthenticationSyncMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check authentication synchronization
        var hasBlazorAuth = context.User.Identity?.IsAuthenticated == true;
        var hasApiCookie = context.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application");

        if (hasBlazorAuth && !hasApiCookie)
        {
            _logger.LogWarning("Blazor authenticated but no API cookie - forcing logout");
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/login");
            return;
        }

        await _next(context);
    }
}