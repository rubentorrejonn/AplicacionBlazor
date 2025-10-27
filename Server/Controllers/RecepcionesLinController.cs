using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecepcionesLinController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RecepcionesLinController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("{albaran}")]
    public async Task<ActionResult<List<RecepcionLineaDto>>> GetRecepcionesLin(int albaran)
    {
        // Traemos las líneas del albarán y hacemos join con Referencias para obtener DesReferencia
        var lineas = await _context.Recepciones_Lin
            .Where(l => l.Albaran == albaran)
            .Join(_context.Referencias,
                  lin => lin.Referencia,
                  refe => refe.Referencia,
                  (lin, refe) => new RecepcionLineaDto
                  {
                      Albaran = lin.Albaran,
                      Linea = lin.Linea,
                      Referencia = lin.Referencia,
                      Cantidad = lin.Cantidad,
                      Bien = lin.Bien,
                      Mal = lin.Mal,
                      DesReferencia = refe.DesReferencia
                  })
            .OrderBy(l => l.Linea)
            .ToListAsync();

        return Ok(lineas);
    }


    [HttpPost]
    public async Task<IActionResult> CreateRecepcionesLin(List<RecepcionLineaDto> lineasDto)
    {
        if (lineasDto == null || !lineasDto.Any())
            return BadRequest("No se proporcionaron líneas.");

        var albaran = lineasDto.First().Albaran;

        if (!await _context.Recepciones_Cab.AnyAsync(c => c.Albaran == albaran))
            return BadRequest($"El albarán {albaran} no existe.");

        // Validar referencias
        var referencias = lineasDto.Select(l => l.Referencia).Distinct().ToList();
        var referenciasValidas = await _context.Referencias
            .Where(r => referencias.Contains(r.Referencia))
            .ToDictionaryAsync(r => r.Referencia, r => r);

        var referenciasInvalidas = referencias.Except(referenciasValidas.Keys).ToList();
        if (referenciasInvalidas.Any())
            return BadRequest($"Referencias no válidas: {string.Join(", ", referenciasInvalidas)}");

        // Iniciar transacción
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var maxLinea = await _context.Recepciones_Lin
                .Where(l => l.Albaran == albaran)
                .MaxAsync(l => (int?)l.Linea) ?? 0;

            // 1. Guardar líneas en Recepciones_Lin
            var entidadesLin = lineasDto.Select((dto, index) => new RecepcionesLin
            {
                Albaran = dto.Albaran,
                Linea = maxLinea + index + 1,
                Referencia = dto.Referencia,
                Cantidad = dto.Cantidad ?? 0,
                Bien = dto.Bien ?? 0,
                Mal = dto.Mal ?? 0
            }).ToList();

            _context.Recepciones_Lin.AddRange(entidadesLin);
            /*
            // 2. Guardar números de serie (si existen)
            foreach (var dto in lineasDto)
            {
                var refInfo = referenciasValidas[dto.Referencia];

                // Bien
                if (dto.NumerosSerieBien?.Any() == true)
                {
                    if (dto.NumerosSerieBien.Count != dto.Bien)
                        return BadRequest($"La cantidad de NSeries para 'Bien' no coincide con la cantidad indicada en {dto.Referencia}.");

                    foreach (var nserie in dto.NumerosSerieBien)
                    {
                        // Validar longitud si aplica
                        if (refInfo.LongNSerie.HasValue && nserie.Length != refInfo.LongNSerie.Value)
                            return BadRequest($"El número de serie '{nserie}' no tiene la longitud esperada ({refInfo.LongNSerie.Value}) para la referencia {dto.Referencia}.");

                        _context.NSeries_Recepciones.Add(new NSeriesRecepciones
                        {
                            NSerie = nserie,
                            Albaran = dto.Albaran,
                            Palet = 0, // Se actualizará al generar palets
                            Referencia = dto.Referencia,
                            FCreacion = DateTime.Now
                        });
                    }
                }

                // Mal
                if (dto.NumerosSerieMal?.Any() == true)
                {
                    if (dto.NumerosSerieMal.Count != dto.Mal)
                        return BadRequest($"La cantidad de NSeries para 'Mal' no coincide con la cantidad indicada en {dto.Referencia}.");

                    foreach (var nserie in dto.NumerosSerieMal)
                    {
                        if (refInfo.LongNSerie.HasValue && nserie.Length != refInfo.LongNSerie.Value)
                            return BadRequest($"El número de serie '{nserie}' no tiene la longitud esperada ({refInfo.LongNSerie.Value}) para la referencia {dto.Referencia}.");

                        _context.NSeries_Recepciones.Add(new NSeriesRecepciones
                        {
                            NSerie = nserie,
                            Albaran = dto.Albaran,
                            Palet = 0,
                            Referencia = dto.Referencia,
                            FCreacion = DateTime.Now
                        });
                    }
                }
            } */

            // 3. Generar palets (agrupando por referencia y estado)
            foreach (var dto in lineasDto)
            {
                var refInfo = referenciasValidas[dto.Referencia];
                var tamanoPalet = 1000;

                // Palets para Bien
                if (dto.Bien > 0)
                {
                    var unidadesBien = dto.Bien.Value;
                    while (unidadesBien > 0)
                    {
                        var cantidadEnPalet = Math.Min(unidadesBien, tamanoPalet);
                        _context.Palets.Add(new Palets
                        {
                            Referencia = dto.Referencia,
                            Cantidad = cantidadEnPalet,
                            Albaran = dto.Albaran,
                            Ubicacion = "UBI-1",
                            Estado = 1, // 1 = Bien
                            FInsert = DateTime.Now
                        });
                        unidadesBien -= cantidadEnPalet;
                    }
                }

                // Palets para Mal
                if (dto.Mal > 0)
                {
                    var unidadesMal = dto.Mal.Value;
                    while (unidadesMal > 0)
                    {
                        var cantidadEnPalet = Math.Min(unidadesMal, tamanoPalet);
                        _context.Palets.Add(new Palets
                        {
                            Referencia = dto.Referencia,
                            Cantidad = cantidadEnPalet,
                            Albaran = dto.Albaran,
                            Ubicacion = "UBI-1",
                            Estado = 2, // 2 = Mal
                            FInsert = DateTime.Now
                        });
                        unidadesMal -= cantidadEnPalet;
                    }
                }
            }
            Console.WriteLine($"Total palets a guardar: {_context.Palets.Local.Count}");
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error al crear líneas: {ex}");
            return StatusCode(500, "Error interno al procesar la recepción.");
        }
    }

    [HttpDelete("{albaran}")]
    public async Task<IActionResult> DeleteRecepcionesLin(int albaran)
    {
        var lineas = await _context.Recepciones_Lin
            .Where(l => l.Albaran == albaran)
            .ToListAsync();
        if (!lineas.Any())
            return NotFound("No se encontraron líneas.");

        _context.Recepciones_Lin.RemoveRange(lineas);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}