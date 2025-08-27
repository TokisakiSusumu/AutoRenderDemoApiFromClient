namespace Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

public abstract partial class UniversalYardifyAuthenticationService
{
    public async Task<bool> LogoutAsync()
    {
        try
        {
            var response = await httpClient.PostAsync("/api/auth/logout", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout failed: {ex.Message}");
            return false;
        }
    }
}