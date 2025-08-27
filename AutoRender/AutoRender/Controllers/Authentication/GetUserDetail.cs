using Microsoft.AspNetCore.Authentication;
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
                            Email = identityInfo.Email,
                            Role = identityInfo.Role, //TODO: find out how to get role
                            IsAuthenticated = true
                        });
                    }
                }
            }
        }

        return Ok(new UserDetailDTO { IsAuthenticated = false });
    }
}