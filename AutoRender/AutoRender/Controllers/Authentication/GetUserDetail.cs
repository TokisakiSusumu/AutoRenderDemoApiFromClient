using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Yardify.Frontend.Client.Interfaces.Authentication;

namespace Yardify.Frontend.Components.Authentication.Controllers.Authentication;

public sealed partial class YardifyAuthenticationController : ControllerBase
{
    [HttpGet("current-user")]
    public async Task<ActionResult<UserDetailDTO>> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var token = HttpContext.Session.GetString("BearerToken");
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                    var response = await _httpClient.GetAsync("manage/info");
                    if (response.IsSuccessStatusCode)
                    {
                        var identityInfo = await response.Content.ReadFromJsonAsync<UserDetailDTO>();
                        if (identityInfo != null)
                        {
                            return Ok(new UserDetailDTO
                            {
                                Email = User.Identity.Name ?? identityInfo.Email,
                                Role = "User", // Default role
                                IsAuthenticated = true
                            });
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Token expired - clear session
                        HttpContext.Session.Clear();
                    }
                }
                catch { }
            }

            // Fallback if API call fails but user is authenticated
            return Ok(new UserDetailDTO
            {
                Email = User.Identity.Name,
                Role = "User",
                IsAuthenticated = true
            });
        }

        return Ok(new UserDetailDTO { IsAuthenticated = false });
    }
}