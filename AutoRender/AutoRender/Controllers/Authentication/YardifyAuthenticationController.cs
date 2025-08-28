using Microsoft.AspNetCore.Mvc;

namespace Yardify.Frontend.Components.Authentication.Controllers.Authentication;

[Route("api/auth")]
[ApiController]
public sealed partial class YardifyAuthenticationController : ControllerBase
{
    private readonly HttpClient _httpClient;
    public YardifyAuthenticationController(IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7191/");
    }
}
