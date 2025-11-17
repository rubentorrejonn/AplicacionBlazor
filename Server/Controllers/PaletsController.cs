using Microsoft.AspNetCore.Mvc;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace UltimateProyect.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaletsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaletsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("disponibles")]
        public async Task <ActionResult<List<VistaPaletsReservados>>> getPaletsDisponibles()
        {
            return await _context.V_MOVIMIENTO_PALETS.ToListAsync();

        }
        [HttpGet("stock")]
        public async Task<ActionResult<Dictionary<string, int>>> GetStockDisponible()
        {
            var stock = await _context.Palets
                .Where(p => p.Estado == 1)
                .Join(_context.Ubicaciones,
                      palet => palet.Ubicacion,
                      ubicacion => ubicacion.Ubicacion,
                      (palet, ubicacion) => new { palet, ubicacion })
                .Where(x => x.ubicacion.StatusUbi == 1)
                .GroupBy(x => x.palet.Referencia)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.palet.Cantidad));

            return Ok(stock);
        }
        [HttpGet("{paletId}")]
        public async Task<ActionResult<Palets>> GetPalet(int paletId)
        {
            var palet = await _context.Palets.FindAsync(paletId);
            if (palet == null)
                return NotFound("Palet no encontrado.");

            return Ok(palet);
        }

        [HttpGet("activas")]
        public async Task<ActionResult<List<Ubicaciones>>> GetUbicacionesActivas()
        {
            var ubicaciones = await _context.Ubicaciones
                .Where(u => u.StatusUbi == 1)
                .ToListAsync();

            return Ok(ubicaciones);
        }
        [HttpPut("{paletId}/mover")]
        [Authorize]
        public async Task<IActionResult> MoverPalet(int paletId, [FromBody] MoverPaletRequestDto request)
        {
            if (string.IsNullOrEmpty(request?.NuevaUbicacion))
                return BadRequest("La nueva ubicación es requerida.");

            var ubicacionValida = await _context.Ubicaciones
                .AnyAsync(u => u.Ubicacion == request.NuevaUbicacion && u.StatusUbi == 1);
            if (!ubicacionValida)
                return BadRequest("La ubicación seleccionada no es válida.");

            var palet = await _context.Palets.FindAsync(paletId);
            if (palet == null)
                return NotFound("Palet no encontrado.");

            palet.Ubicacion = request.NuevaUbicacion;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Palet {paletId} movido a {request.NuevaUbicacion}." });
        }

        public class MoverPaletRequestDto
        {
            public string NuevaUbicacion { get; set; } = null!;
        }

    }
}
