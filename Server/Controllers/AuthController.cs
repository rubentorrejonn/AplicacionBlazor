using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequestDto model)
    {
        try
        {
            if (model == null)
            {
                return BadRequest(new LoginResultDto { Success = false, Message = "Datos de login inválidos." });
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new LoginResultDto { Success = false, Message = "Usuario y contraseña son requeridos." });
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.UserName == model.Username);
            if (user == null)
            {
                return Unauthorized(new LoginResultDto { Success = false, Message = "Credenciales incorrectas." });
            }

            if (user.Password != model.Password)
            {
                return Unauthorized(new LoginResultDto { Success = false, Message = "Credenciales incorrectas." });
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
            new Claim(ClaimTypes.Role, user.Role ?? "Usuario")
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            });

            return Ok(new LoginResultDto
            {
                Success = true,
                Message = "Inicio de sesión exitoso.",
                UserName = user.UserName,
                UserId = user.IdUsuario
            });
        }
        catch (Exception ex)
        {
            // Esto te mostrará el error real en la consola del servidor
            Console.WriteLine($"Error en AuthController.Login: {ex}");
            return StatusCode(500, new LoginResultDto { Success = false, Message = "Error interno del servidor." });
        }
    }

    [HttpGet("test")]
    public async Task<ActionResult> Test()
    {
        try
        {
            var users = await _context.Usuarios.ToListAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { Message = "Sesión cerrada correctamente." });
    }
}