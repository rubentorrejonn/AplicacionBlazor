// Client/Services/CustomAuthStateProvider.cs
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Client.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;

    public CustomAuthStateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/user/info");
            if (response.IsSuccessStatusCode)
            {
                var userInfoJson = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<UserInfoResponseDto>(userInfoJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userInfo != null && userInfo.IsAuthenticated)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, userInfo.UserName ?? ""),
                        new Claim(ClaimTypes.NameIdentifier, userInfo.UserId?.ToString() ?? ""),
                    }, "apiauth_type");

                    if (userInfo.Roles?.Any() == true)
                    {
                        foreach (var role in userInfo.Roles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, userInfo.Roles));
                        }
                    }

                    var user = new ClaimsPrincipal(identity);
                    return new AuthenticationState(user);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error al obtener estado de autenticación: {ex.Message}");
        }

        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymousUser);
    }

    public void NotifyUserAuthentication(string userName)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, "apiauth_type"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }
}