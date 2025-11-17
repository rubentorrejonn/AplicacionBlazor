// Server/Controllers/IcpController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var cabecera = await _context.Orden_Salida_Cab.FindAsync(verificacion.Peticion);
        if (cabecera == null || cabecera.Estado != 2)
            return BadRequest("La petición no está en un estado válido para verificación.");

        foreach (var linea in verificacion.Lineas)
        {
            if (linea.RequiereNSerie.HasValue)
            {
                if (linea.NumerosSerie.Count != linea.Cantidad)
                    return BadRequest($"La línea {linea.Linea} debe tener exactamente {linea.Cantidad} números de serie.");

                if (linea.LongNSerie.HasValue)
                {
                    foreach (var nserie in linea.NumerosSerie)
                    {
                        if (string.IsNullOrEmpty(nserie) || nserie.Length != linea.LongNSerie.Value)
                            return BadRequest($"El número de serie '{nserie}' no cumple con la longitud requerida de {linea.LongNSerie.Value}.");
                    }
                }
            }
        }

        cabecera.Estado = 3;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Verificación completada correctamente." });
    }
}