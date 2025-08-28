using System.Net.Http.Json;

namespace Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

public abstract partial class UniversalYardifyAuthenticationService
{
    public virtual async Task<bool> LoginAsync(LoginRequestDTO loginRequest)
    {
        // Always route through the API controller to handle session properly
        // This avoids trying to set session during response rendering
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed: {ex.Message}");
            return false;
        }
    }
}