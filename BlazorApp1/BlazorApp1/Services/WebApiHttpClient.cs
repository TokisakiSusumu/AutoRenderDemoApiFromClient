using System.Net;

namespace BlazorApp1.Services
{
    public class WebApiHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<WebApiHttpClient> _logger;

        public WebApiHttpClient(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WebApiHttpClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public HttpClient CreateUnauthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7191/");
            return client;
        }
        public async Task<HttpResponseMessage> LoginAsync(string email, string password)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AllowAutoRedirect = false
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7191/")
            };

            var loginRequest = new { email, password };
            var response = await client.PostAsJsonAsync(
                "login?useCookies=true&useSessionCookies=false",
                loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var cookies = handler.CookieContainer.GetCookies(new Uri("https://localhost:7191/"));
                var apiCookie = cookies[".AspNetCore.Identity.Application"];

                if (apiCookie != null)
                {
                    // Store the cookie and expiration info in the response for the caller to handle
                    response.Headers.Add("X-API-Cookie-Value", apiCookie.Value);
                }
            }

            return response;
        }

        public async Task<bool> LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request.Cookies.TryGetValue(".AspNetCore.Identity.Application", out var apiCookie) == true)
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

                var response = await client.PostAsync("logout", null);
                return response.IsSuccessStatusCode;
            }
            return false;
        }
    }
}