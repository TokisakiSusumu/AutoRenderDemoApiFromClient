using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yardify.Frontend.Client.Interfaces.Authentication;

namespace Yardify.Frontend.Components.Authentication.Controllers.Authentication;

public sealed partial class YardifyAuthenticationController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("login", new
            {
                email = loginRequest.Email,
                password = loginRequest.Password
            });

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
                if (loginResponse != null)
                {
                    // Store tokens
                    HttpContext.Session.SetString("BearerToken", loginResponse.AccessToken);
                    HttpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken);

                    // NEW: Store token expiration time
                    var tokenExpiration = DateTimeOffset.UtcNow.AddSeconds(loginResponse.ExpiresIn);
                    HttpContext.Session.SetString("TokenExpiration", tokenExpiration.ToString("o"));

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, loginRequest.Email),
                        new Claim(ClaimTypes.Name, loginRequest.Email)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = tokenExpiration
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return Ok(new { success = true });
                }
            }

            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred during login" });
        }
    }
}