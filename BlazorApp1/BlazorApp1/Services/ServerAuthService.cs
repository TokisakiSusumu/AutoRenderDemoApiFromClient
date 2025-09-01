using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net;

namespace BlazorApp1.Services
{
    public class ServerAuthService : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ServerAuthService> _logger;

        public ServerAuthService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ServerAuthService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogError("No HttpContext available for login");
                    return false;
                }

                // IMPORTANT: Clean up any existing API session before creating a new one
                // This prevents zombie sessions in the API
                await CleanupExistingApiSessionAsync(httpContext);

                // Now proceed with fresh login
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    UseCookies = true,
                    AllowAutoRedirect = false
                };

                using var apiClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:7191/")
                };

                var loginRequest = new { email, password };
                var response = await apiClient.PostAsJsonAsync(
                    "login?useCookies=true&useSessionCookies=false",
                    loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    // Extract the API cookie from the response
                    var cookies = handler.CookieContainer.GetCookies(new Uri("https://localhost:7191/"));
                    var apiCookie = cookies[".AspNetCore.Identity.Application"];

                    if (apiCookie != null && !httpContext.Response.HasStarted)
                    {
                        // Forward the API cookie to the browser
                        // The browser needs this for future API calls
                        httpContext.Response.Cookies.Append(
                            ".AspNetCore.Identity.Application",
                            apiCookie.Value,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Lax,
                                // Don't set Expires here - let the API control its own expiration
                                // This way the browser just forwards whatever the API sent
                            });

                        // Create Blazor authentication cookie for local state
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, email),
                            new Claim(ClaimTypes.Email, email),
                            // Add a session ID to track this specific login session
                            new Claim("SessionId", Guid.NewGuid().ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims,
                            CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10)
                        };

                        await httpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation(
                            "Login successful for {Email}. Session: {SessionId}",
                            email,
                            claims.First(c => c.Type == "SessionId").Value);

                        return true;
                    }
                }

                _logger.LogWarning("Login failed for {Email}. Status: {StatusCode}",
                    email, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", email);
                return false;
            }
        }

        private async Task CleanupExistingApiSessionAsync(HttpContext httpContext)
        {
            try
            {
                // Check if we have an existing API cookie
                if (!httpContext.Request.Cookies.TryGetValue(".AspNetCore.Identity.Application", out var existingApiCookie))
                {
                    _logger.LogDebug("No existing API session to clean up");
                    return;
                }

                _logger.LogInformation("Cleaning up existing API session before new login");

                // Create a handler with the existing cookie
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    UseCookies = true
                };

                // Add the existing cookie to the container
                handler.CookieContainer.Add(
                    new Uri("https://localhost:7191/"),
                    new Cookie(".AspNetCore.Identity.Application", existingApiCookie));

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:7191/")
                };

                // Call the logout endpoint to clean up server-side state
                var logoutResponse = await client.PostAsync("logout", null);

                if (logoutResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully cleaned up old API session");
                }
                else
                {
                    _logger.LogWarning(
                        "API logout returned {StatusCode} during cleanup",
                        logoutResponse.StatusCode);
                }

                // Clear the cookie from the browser regardless of API response
                httpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");
            }
            catch (Exception ex)
            {
                // Don't fail the login if cleanup fails
                _logger.LogWarning(ex, "Failed to cleanup existing API session, continuing with login");
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                // First, try to logout from API if we have the cookie
                if (httpContext.Request.Cookies.TryGetValue(".AspNetCore.Identity.Application", out var apiCookie))
                {
                    try
                    {
                        var handler = new HttpClientHandler
                        {
                            CookieContainer = new CookieContainer(),
                            UseCookies = true
                        };

                        handler.CookieContainer.Add(
                            new Uri("https://localhost:7191/"),
                            new Cookie(".AspNetCore.Identity.Application", apiCookie));

                        using var client = new HttpClient(handler)
                        {
                            BaseAddress = new Uri("https://localhost:7191/")
                        };

                        await client.PostAsync("logout", null);
                        _logger.LogInformation("API logout successful");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to call API logout endpoint");
                    }
                }

                // Clear both cookies from the browser
                httpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");
                httpContext.Response.Cookies.Delete("BlazorAuthCookie");

                // Sign out from Blazor authentication
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("User logged out - all sessions cleaned");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }

        public Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                // If we're authenticated locally and OnValidatePrincipal hasn't rejected us,
                // then we know both cookies are valid
                return Task.FromResult(new AuthenticationState
                {
                    IsAuthenticated = true,
                    UserName = httpContext.User.Identity.Name
                });
            }

            return Task.FromResult(new AuthenticationState
            {
                IsAuthenticated = false
            });
        }
    }
}