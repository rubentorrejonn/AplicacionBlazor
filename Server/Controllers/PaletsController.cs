using Microsoft.AspNetCore.Mvc;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult<Dictionary<string, int>>> getStockDisponible()
        {
                var stock = await _context.Palets
                    .Where(p => p.Estado == 1)
                    .GroupBy(p => p.Referencia)
                    .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Cantidad));

                return Ok(stock);
        }
    }
}
