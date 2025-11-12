using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("info")]
    public async Task<ActionResult<UserInfoResponseDto>> GetUserInfo()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Ok(new UserInfoResponseDto
            {
                IsAuthenticated = false,
                UserName = null,
                UserId = null,
                Roles = null
            });
        }

        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
        if (user == null)
        {
            return Ok(new UserInfoResponseDto
            {
                IsAuthenticated = false,
                UserName = null,
                UserId = null,
                Roles = null
            });
        }

        var userInfo = new UserInfoResponseDto
        {
            IsAuthenticated = true,
            UserName = user.UserName,
            UserId = user.IdUsuario,
            Roles = user.Role
        };

        return Ok(userInfo);
    }
}