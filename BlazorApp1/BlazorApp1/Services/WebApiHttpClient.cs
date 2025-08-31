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

        public HttpClient CreateAuthenticatedClient()
        {
            // Create a fresh HttpClient instance for this request
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:7191/");

            // Get the current HTTP context for THIS specific request
            var httpContext = _httpContextAccessor.HttpContext;

            // Extract THIS user's API cookie from THEIR request
            if (httpContext?.Request.Cookies.TryGetValue(".AspNetCore.Identity.Application", out var apiCookie) == true)
            {
                // Add THIS user's cookie to THIS HttpClient instance
                client.DefaultRequestHeaders.Add("Cookie",
                    $".AspNetCore.Identity.Application={apiCookie}");

                var sessionId = httpContext.User.FindFirst("SessionId")?.Value;
                _logger.LogDebug(
                    "Created authenticated HttpClient for session {SessionId}",
                    sessionId);
            }
            else
            {
                _logger.LogWarning("No API cookie available - API calls will likely fail with 401");
            }

            return client;
        }
    }
}