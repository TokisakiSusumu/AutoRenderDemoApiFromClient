using System.Net.Http.Json;

namespace Yardify.Frontend.Client.Interfaces.Authentication.UniversalRequests;

public abstract partial class UniversalYardifyAuthenticationService
{
    public async Task<UserDetailDTO?> GetCurrentUserInfoAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<UserDetailDTO>("api/auth/current-user");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Get current user failed: {ex.Message}");
            return null;
        }
    }
}