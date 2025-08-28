using Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

namespace AutoRender;

public class ServerAuthService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) :
    UniversalYardifyAuthenticationService(CreateHttpClient(httpClientFactory, httpContextAccessor))
{
    private static HttpClient CreateHttpClient(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        var httpClient = httpClientFactory.CreateClient();
        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            httpClient.BaseAddress = new Uri(
                $"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
        }
        return httpClient;
    }
}