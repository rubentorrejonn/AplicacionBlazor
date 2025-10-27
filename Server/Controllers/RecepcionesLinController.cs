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

        var referencias = lineasDto.Select(l => l.Referencia).Distinct().ToList();
        var referenciasValidas = await _context.Referencias
            .Where(r => referencias.Contains(r.Referencia))
            .ToDictionaryAsync(r => r.Referencia, r => r);

        var referenciasInvalidas = referencias.Except(referenciasValidas.Keys).ToList();
        if (referenciasInvalidas.Any())
            return BadRequest($"Referencias no válidas: {string.Join(", ", referenciasInvalidas)}");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Guardar líneas (con cálculo seguro de Linea)
            var maxLinea = await _context.Recepciones_Lin
                .Where(l => l.Albaran == albaran)
                .MaxAsync(l => (int?)l.Linea) ?? 0;

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
            await _context.SaveChangesAsync();

            // 2. Generar palets y guardarlos
            var paletsPorLinea = new List<(RecepcionLineaDto Dto, List<Palets> Palets)>();
            foreach (var dto in lineasDto)
            {
                var palets = new List<Palets>();

                // Palets para Bien
                if (dto.Bien > 0)
                {
                    var unidades = dto.Bien.Value;
                    while (unidades > 0)
                    {
                        var cant = Math.Min(unidades, 1000);
                        palets.Add(new Palets
                        {
                            Referencia = dto.Referencia,
                            Cantidad = cant,
                            Albaran = dto.Albaran,
                            Ubicacion = "UBI-1",
                            Estado = 1,
                            FInsert = DateTime.Now
                        });
                        unidades -= cant;
                    }
                }

                // Palets para Mal
                if (dto.Mal > 0)
                {
                    var unidades = dto.Mal.Value;
                    while (unidades > 0)
                    {
                        var cant = Math.Min(unidades, 1000);
                        palets.Add(new Palets
                        {
                            Referencia = dto.Referencia,
                            Cantidad = cant,
                            Albaran = dto.Albaran,
                            Ubicacion = "UBI-1",
                            Estado = 2,
                            FInsert = DateTime.Now
                        });
                        unidades -= cant;
                    }
                }

                paletsPorLinea.Add((dto, palets));
                _context.Palets.AddRange(palets);
            }

            await _context.SaveChangesAsync(); //

            // 3. Guardar números de serie (con Palet real)
            foreach (var (dto, palets) in paletsPorLinea)
            {
                int idx = 0;

                // Bien
                if (dto.NumerosSerieBien?.Any() == true)
                {
                    foreach (var nserie in dto.NumerosSerieBien)
                    {
                        _context.NSeries_Recepciones.Add(new NSeriesRecepciones
                        {
                            NSerie = nserie,
                            Albaran = dto.Albaran,
                            Palet = palets[idx].Palet,
                            Referencia = dto.Referencia,
                            FCreacion = DateTime.Now
                        });
                        idx++;
                    }
                }

                // Mal
                if (dto.NumerosSerieMal?.Any() == true)
                {
                    foreach (var nserie in dto.NumerosSerieMal)
                    {
                        _context.NSeries_Recepciones.Add(new NSeriesRecepciones
                        {
                            NSerie = nserie,
                            Albaran = dto.Albaran,
                            Palet = palets[idx].Palet,
                            Referencia = dto.Referencia,
                            FCreacion = DateTime.Now
                        });
                        idx++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error: {ex}");
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