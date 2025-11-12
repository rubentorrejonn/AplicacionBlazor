using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json; // Para ReadFromJsonAsync
using System.Security.Claims;
using System.Text.Json;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Client.Services
{
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

                    if (userInfo != null && userInfo.IsAuthenticated) // ✅ Esto se evalúa como false si IsAuthenticated = false
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
                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                            }
                        }

                        var user = new ClaimsPrincipal(identity);
                        return new AuthenticationState(user);
                    }
                }
                // Si no es exitoso (401, 403, 500, etc.) o IsAuthenticated es false, devuelve usuario anónimo
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[ERROR] HttpRequestException al obtener info de usuario: {httpEx.Message}");
            }
            catch (TaskCanceledException tcEx)
            {
                Console.WriteLine($"[ERROR] TaskCanceledException (timeout) al obtener info de usuario: {tcEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                // ESTE ES EL ERROR QUE ESTÁS VIENDO
                Console.WriteLine($"[ERROR] JsonException al deserializar api/user/info: {jsonEx.Message}");
                Console.WriteLine($"[ERROR] Contenido recibido (posiblemente HTML):");
                try
                {
                    // Intentar leer el contenido que causó el error
                    var errorContent = await _httpClient.GetAsync("api/user/info"); // Vuelve a hacer la llamada
                    var rawContent = await errorContent.Content.ReadAsStringAsync();
                    Console.WriteLine(rawContent);
                }
                catch
                {
                    Console.WriteLine("No se pudo leer el contenido de la respuesta fallida.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Excepción general al obtener estado de autenticación: {ex.Message}");
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
}