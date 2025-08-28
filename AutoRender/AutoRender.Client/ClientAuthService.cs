using Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

namespace AutoRender.Client;
public class ClientAuthService(HttpClient httpClient) : UniversalYardifyAuthenticationService(httpClient)
{
}
