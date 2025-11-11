using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdenSalidaLinController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdenSalidaLinController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrdenSalidaLin([FromBody] List<OrdenSalidaLinDto> lineasDto)
    {
        if (lineasDto == null || !lineasDto.Any())
            return BadRequest("No se proporcionaron líneas para el pedido.");

        var peticion = lineasDto.First().Peticion;
        if (lineasDto.Any(l => l.Peticion != peticion))
            return BadRequest("Todas las líneas del pedido deben pertenecer a la misma petición.");

        var cabecera = await _context.Orden_Salida_Cab.FindAsync(peticion);
        if (cabecera == null)
            return NotFound($"La petición {peticion} no existe.");

        if (cabecera.Estado != 0 && cabecera.Estado != 1)
            return BadRequest($"No se pueden añadir líneas a una petición en estado {cabecera.Estado}.");

        var referencias = lineasDto.Select(l => l.Referencia).Distinct().ToList();
        var referenciasValidas = await _context.Referencias
            .Where(r => referencias.Contains(r.Referencia))
            .ToDictionaryAsync(r => r.Referencia, r => r);

        var referenciasInvalidas = referencias.Except(referenciasValidas.Keys).ToList();
        if (referenciasInvalidas.Any())
            return BadRequest($"Referencias no válidas: {string.Join(", ", referenciasInvalidas)}");

        try
        {
            var lineasJson = JsonSerializer.Serialize(lineasDto);

            foreach (var dto in lineasDto)
            {

                var retCodeParam = new SqlParameter("@RETCODE", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC @RETCODE = PA_GUARDAR_PEDIDO @PETICION, @REFERENCIA, @CANTIDAD, @INVOKER, @USUARIO, @CULTURA, @RETCODE OUTPUT, @MENSAJE OUTPUT",
                    new SqlParameter("@PETICION", dto.Peticion),
                    new SqlParameter("@REFERENCIA", dto.Referencia),
                    new SqlParameter("@CANTIDAD", dto.Cantidad),
                    new SqlParameter("@INVOKER", ""),
                    new SqlParameter("@USUARIO", ""),
                    new SqlParameter("@CULTURA", ""),
                    retCodeParam,
                    mensajeParam
                );

                var retCode = (int)retCodeParam.Value;
                var mensaje = (string)mensajeParam.Value;

                if (retCode != 0)
                {
                    Console.WriteLine($"[ERROR] PA_GUARDAR_PEDIDO  falló con RETCODE: {retCode}. Mensaje: {mensaje}");
                    return StatusCode(500, $"Error al procesar el pedido: {mensaje}");
                }
            }
                return NoContent();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            Console.WriteLine($"[ERROR] SqlException al ejecutar PA_GUARDAR_PEDIDO: {sqlEx.Message}");
            return StatusCode(500, $"Error interno al procesar el pedido: {sqlEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Excepción general al ejecutar PA_GUARDAR_PEDIDO: {ex.Message}");
            return StatusCode(500, "Error interno al procesar el pedido.");
        }
    }

    [HttpGet("{peticion}")]
    public async Task<ActionResult<List<OrdenSalidaLinDto>>> GetOrdenSalidaLin(int peticion)
    {
        var lineas = await _context.Orden_Salida_Lin
            .Where(l => l.Peticion == peticion)
            .Join(_context.Referencias,
                  lin => lin.Referencia,
                  refe => refe.Referencia,
                  (lin, refe) => new OrdenSalidaLinDto
                  {
                      Peticion = lin.Peticion,
                      Linea = lin.Linea,
                      Referencia = lin.Referencia,
                      Cantidad = lin.Cantidad,
                      DesReferencia = refe.DesReferencia
                  })
            .OrderBy(l => l.Linea)
            .ToListAsync();

        return Ok(lineas);
    }
}