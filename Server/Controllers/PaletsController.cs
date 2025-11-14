using Microsoft.AspNetCore.Mvc;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

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
        public async Task<IActionResult> MoverPalet(int paletId, [FromBody] MoverPaletRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.NuevaUbicacion))
                return BadRequest("Solicitud inválida o nueva ubicación no especificada.");

            var palet = await _context.Palets.FindAsync(paletId);
            if (palet == null)
                return NotFound("Palet no encontrado.");

            var ubicacionValida = await _context.Ubicaciones.AnyAsync(u => u.Ubicacion == request.NuevaUbicacion && u.StatusUbi == 1);
            if (!ubicacionValida)
                return BadRequest("La nueva ubicación no es válida o no está activa.");

            palet.Ubicacion = request.NuevaUbicacion;
            _context.Palets.Update(palet);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = $"Palet {paletId} movido a {request.NuevaUbicacion} correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error al mover el palet: {ex.Message}" });
            }
        }
        public class MoverPaletRequestDto
        {
            public string NuevaUbicacion { get; set; } = null!;
        }
        [HttpPost("registrar-movimiento-log")]
        public async Task<IActionResult> RegistrarMovimientoLog([FromBody] RegistrarMovimientoLogRequestDto request)
        {
            try
            {
                var retCodeParam = new SqlParameter("@RETCODE", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var mensajeParma = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXE @RETCODE = PA_MOVIMIENTOS_LOG " +
                    "@PETICION, @PALET, @REFERENCIA " +
                    "@UBICACION_ORIGEN, @UBICACION_DESTINO, @FECHA_MOVIMIENTO, @ID_USUARIO " +
                    "@INVOKER, @USUARIO, @CULTURA " +
                    "@RETCODE OUTPUT, @MENSAJE OUTPUT",
                    new SqlParameter("@PETICION", request.Peticion),
                    new SqlParameter("@PALET", request.Palet),
                    new SqlParameter("@REFERENCIA", request.Referencia),
                    new SqlParameter("@UBICACION_ORIGEN", request.UbicacionOrigen),
                    new SqlParameter("@UBICACION_DESTINO", request.UbicacionDestino),
                    new SqlParameter("@FECHA_MOVIMIENTO", request.FechaMovimiento),
                    new SqlParameter("@ID_USUARIO", request.IdUsuario),
                    new SqlParameter("@INVOKER", ""),
                    new SqlParameter("@USUARIO", ""),
                    new SqlParameter("@CULTURA", ""),
                    retCodeParam,
                    mensajeParma
            );
                var retCode = (int)retCodeParam.Value;
                var mensaje = (string)mensajeParma.Value;

                Console.WriteLine($"[INFO] Movimiento registrado correctamente. RETCODE: {retCode}. Mensaje: {mensaje}");
                return Ok(new { Message = mensaje });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Excepción general al registrar movimiento log: {ex.Message}");
                Console.WriteLine($"[ERROR] Detalles: {ex}");
                return StatusCode(500, new { Message = $"Error interno al registrar el movimiento: {ex.Message}" });
            }
        }
    }
}
