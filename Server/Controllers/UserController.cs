using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UltimateProyect.Server.Models;
using UltimateProyect.Shared.Models; 

namespace UltimateProyect.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("info")] // GET /api/user/info
    // ❌ NO usar [Authorize] aquí si quieres que devuelva un JSON coherente
    public async Task<ActionResult<UserInfoResponseDto>> GetUserInfo()
    {
        // ✅ Comprobar si el usuario está autenticado
        if (!User.Identity.IsAuthenticated)
        {
            // ✅ Devolver JSON con IsAuthenticated = false
            return Ok(new UserInfoResponseDto
            {
                IsAuthenticated = false,
                UserName = null,
                UserId = null,
                Roles = new List<string>() // O new string[0]
            });
        }

        // ✅ Si está autenticado, obtener datos
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // Caso raro: autenticado pero no se encuentra el usuario
            return Ok(new UserInfoResponseDto
            {
                IsAuthenticated = false,
                UserName = null,
                UserId = null,
                Roles = new List<string>()
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userInfo = new UserInfoResponseDto
        {
            IsAuthenticated = true,
            UserName = user.UserName,
            UserId = user.Id,
            Roles = roles.ToList()
        };

        return Ok(userInfo);
    }
}