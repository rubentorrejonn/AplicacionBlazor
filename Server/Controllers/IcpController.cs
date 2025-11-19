using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IcpController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public IcpController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("salidas-disponibles")]
    public async Task<ActionResult<List<VistaOrdenSalidaCab>>> GetSalidasDisponibles()
    {
        var salidas = await _context.V_OSC_ESTADO_DESCRIPCION
            .Where(s => s.Estado == 2)
            .ToListAsync();
        return Ok(salidas);
    }

    [HttpGet("verificar/{peticion}")]
    public async Task<ActionResult<VerificacionIcpDto>> GetVerificacion(int peticion)
    {
        var cabecera = await _context.V_OSC_ESTADO_DESCRIPCION
            .FirstOrDefaultAsync(c => c.Peticion == peticion && c.Estado == 2);
        if (cabecera == null)
            return NotFound("La petición no existe o no está en estado de verificación.");

        var lineas = await _context.Orden_Salida_Lin
            .Where(l => l.Peticion == peticion)
            .Join(_context.Referencias,
                  lin => lin.Referencia,
                  refe => refe.Referencia,
                  (lin, refe) => new LineaVerificacionDto
                  {
                      Linea = lin.Linea,
                      Referencia = lin.Referencia,
                      DesReferencia = refe.DesReferencia,
                      Cantidad = lin.Cantidad,
                      RequiereNSerie = refe.NSerie,
                      LongNSerie = refe.LongNSerie
                  })
            .OrderBy(l => l.Linea)
            .ToListAsync();

        var verificacion = new VerificacionIcpDto
        {
            Peticion = peticion,
            NombreCliente = cabecera.NombreCliente,
            Lineas = lineas
        };

        return Ok(verificacion);
    }

    [HttpPost("confirmar-verificacion")]
    public async Task<IActionResult> ConfirmarVerificacion([FromBody] VerificacionIcpDto verificacion)
    {
        if (verificacion == null || !verificacion.Lineas.Any())
            return BadRequest("Solicitud inválida.");

        var cabecera = await _context.Orden_Salida_Cab.FindAsync(verificacion.Peticion);
        if (cabecera == null || cabecera.Estado != 2)
            return NotFound("La petición no existe o no está en estado de verificación.");

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userIdActual))
        {
            Console.WriteLine("[ERROR] Usuario no autenticado o ID inválido.");
            return Unauthorized("Usuario no autenticado.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var linea in verificacion.Lineas)
            {
                var cantidadNecesaria = linea.Cantidad;
                var paletsDisponibles = await _context.Palets
                    .Where(p => p.Referencia == linea.Referencia && p.Estado == 3)
                    .OrderBy(p => p.Palet)
                    .ToListAsync();

                foreach (var palet in paletsDisponibles)
                {
                    if (cantidadNecesaria <= 0) break;

                    var cantidadARestar = Math.Min(cantidadNecesaria, palet.Cantidad);

                    palet.Cantidad -= cantidadARestar;
                    cantidadNecesaria -= cantidadARestar;

                    var log = new PickingLogs
                    {
                        Peticion = verificacion.Peticion,
                        Palet = palet.Palet,
                        Referencia = palet.Referencia,
                        CantidadQuitada = cantidadARestar,
                        FechaVerificacion = DateTime.Now,
                        IdUsuario = userIdActual
                    };
                    _context.PickingLog.Add(log);

                    if (palet.Cantidad == 0)
                    {
                        palet.Estado = 5;
                    }
                    else
                    {
                        palet.Estado = 1;
                    }
                    _context.Palets.Update(palet);
                }

                if (cantidadNecesaria > 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"No hay suficiente stock para la referencia {linea.Referencia}.");
                }
            }

            cabecera.Estado = 3;
            _context.Orden_Salida_Cab.Update(cabecera);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Verificación completada correctamente." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[ERROR] ConfirmarVerificacion: {ex.Message}");
            return StatusCode(500, new { Message = "Error interno al confirmar la verificación." });
        }
    }

    [HttpGet("log/{peticion}")]
    public async Task<ActionResult<List<PickingLogs>>> GetPickingLog(int peticion)
    {
        var logs = await _context.PickingLog
            .Where(l => l.Peticion == peticion)
            .OrderBy(l => l.FechaVerificacion)
            .ToListAsync();
        return Ok(logs);
    }
}