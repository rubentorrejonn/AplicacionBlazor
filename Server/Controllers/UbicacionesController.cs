using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Shared.Models;
using UltimateProyect.Server.Data;

namespace UltimateProyect.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UbicacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UbicacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Ubicaciones>>> GetUbicaciones()
        {
            return await _context.Ubicaciones.ToListAsync();
        }
        [HttpGet("{ubicacion}")]
        public async Task<ActionResult<Ubicaciones>> GetUbicaciones(string ubicacion)
        {
            var ubi = await _context.Ubicaciones.FindAsync(ubicacion);
            if (ubicacion == null) return NotFound("Ubicación no encontrada");
            return ubi;
        }
    }
}
