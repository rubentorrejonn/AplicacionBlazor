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
                      Bien = 0,
                      Mal = 0,
                      DesReferencia = refe.DesReferencia,
                      RequiereNSerie = refe.NSerie == true,
                      LongNSerie = refe.LongNSerie
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
            var maxLinea = await _context.Recepciones_Lin
                .Where(l => l.Albaran == albaran)
                .MaxAsync(l => (int?)l.Linea) ?? 0;

            var entidadesLin = lineasDto.Select((dto, index) => new RecepcionesLin
            {
                Albaran = dto.Albaran,
                Linea = maxLinea + index + 1,
                Referencia = dto.Referencia,
                Cantidad = dto.Cantidad ?? 0
                // ⚠️ Sin Bien, sin Mal
            }).ToList();

            _context.Recepciones_Lin.AddRange(entidadesLin);
            await _context.SaveChangesAsync();

            // Generar palets TEMPORALES (solo para tener stock, todos como "Bien" por ahora)
            var palets = new List<Palets>();
            foreach (var lin in entidadesLin)
            {
                var unidades = lin.Cantidad;
                while (unidades > 0)
                {
                    var cant = Math.Min(unidades, 1000);
                    palets.Add(new Palets
                    {
                        Referencia = lin.Referencia,
                        Cantidad = cant,
                        Albaran = lin.Albaran,
                        Ubicacion = "UBI-1",
                        Estado = 1, // temporal: todo bien
                        FInsert = DateTime.Now
                    });
                    unidades -= cant;
                }
            }

            _context.Palets.AddRange(palets);
            await _context.SaveChangesAsync();

            // Actualizar estado a "Recepcionado"
            var cab = await _context.Recepciones_Cab.FindAsync(albaran);
            if (cab != null)
            {
                cab.Estado = 2;
                cab.DesEstado = "Recepcionado";
                _context.Update(cab);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error: {ex}");
            return StatusCode(500, "Error al crear recepción.");
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
    public class confirmarRequest
    {
        public string ubicacion { get; set; } = null!;
        public List<RecepcionLineaDto> Lineas { get; set; }
    }
    public class ConfirmarIcpRequest
    {
        public string Ubicacion { get; set; } = null!;
        public List<RecepcionLineaDto> Lineas { get; set; } = new();
    }

    [HttpPut("confirmar-icp/{albaran}")]
    public async Task<IActionResult> ConfirmarIcp(int albaran, [FromBody] ConfirmarIcpRequest request)
    {
        if (request == null)
            return BadRequest("Solicitud inválida.");

        var lineasDto = request.Lineas;
        var ubicacion = request.Ubicacion;

        if (lineasDto == null || !lineasDto.Any())
            return BadRequest("No se proporcionaron líneas.");

        if (string.IsNullOrWhiteSpace(ubicacion))
            return BadRequest("La ubicación es obligatoria.");

        var cabecera = await _context.Recepciones_Cab.FindAsync(albaran);
        if (cabecera == null)
            return NotFound("Albarán no encontrado.");

        if (cabecera.Estado != 3)
            return BadRequest("Solo se puede confirmar un albarán en estado 'Enviado a ICP'.");

        if (lineasDto.Any(l => l.Albaran != albaran))
            return BadRequest("Todas las líneas deben pertenecer al mismo albarán.");

        var referencias = lineasDto.Select(l => l.Referencia).Distinct().ToList();
        var referenciasValidas = await _context.Referencias
            .Where(r => referencias.Contains(r.Referencia))
            .ToDictionaryAsync(r => r.Referencia, r => r);

        var referenciasInvalidas = referencias.Except(referenciasValidas.Keys).ToList();
        if (referenciasInvalidas.Any())
            return BadRequest($"Referencias no válidas: {string.Join(", ", referenciasInvalidas)}");

        // Validar que cada línea exista en la base de datos
        var lineasExistentes = await _context.Recepciones_Lin
            .Where(l => l.Albaran == albaran)
            .ToDictionaryAsync(l => l.Linea, l => l);

        foreach (var dto in lineasDto)
        {
            if (!lineasExistentes.TryGetValue(dto.Linea, out var lineaDb))
                return BadRequest($"Línea {dto.Linea} no encontrada para el albarán {albaran}.");

            if (lineaDb.Referencia != dto.Referencia)
                return BadRequest($"La referencia de la línea {dto.Linea} no coincide con la registrada.");

            if (dto.Bien + dto.Mal != lineaDb.Cantidad)
                return BadRequest($"La suma de Bien ({dto.Bien}) + Mal ({dto.Mal}) debe ser igual a la cantidad original ({lineaDb.Cantidad}) en la línea {dto.Linea}.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Eliminar palets anteriores del albarán
            var paletsAnteriores = await _context.Palets.Where(p => p.Albaran == albaran).ToListAsync();
            _context.Palets.RemoveRange(paletsAnteriores);
            await _context.SaveChangesAsync();

            // 2. Crear nuevos palets según Bien/Mal
            var nuevosPalets = new List<Palets>();
            foreach (var dto in lineasDto)
            {
                // Palets para Bien
                if (dto.Bien > 0)
                {
                    var unidades = dto.Bien.Value;
                    while (unidades > 0)
                    {
                        var cant = Math.Min(unidades, 1000);
                        nuevosPalets.Add(new Palets
                        {
                            Referencia = dto.Referencia,
                            Cantidad = cant,
                            Albaran = albaran,
                            Ubicacion = ubicacion,
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
                        nuevosPalets.Add(new Palets
                        {
                            Referencia = dto.Referencia,
                            Cantidad = cant,
                            Albaran = albaran,
                            Ubicacion = "UBI-4",
                            Estado = 2,
                            FInsert = DateTime.Now
                        });
                        unidades -= cant;
                    }
                }
            }

            _context.Palets.AddRange(nuevosPalets);
            await _context.SaveChangesAsync();

            // 3. Guardar números de serie si la referencia los requiere
            foreach (var dto in lineasDto)
            {
                var refData = referenciasValidas[dto.Referencia];
                if (!refData.NSerie.HasValue || !refData.NSerie.Value)
                    continue;

                var paletsDeEstaRef = nuevosPalets
                    .Where(p => p.Referencia == dto.Referencia)
                    .OrderBy(p => p.Palet)
                    .ToList();

                var todosNSeries = dto.NumerosSerieBien.Concat(dto.NumerosSerieMal).ToList();
                var esperado = (dto.Bien ?? 0) + (dto.Mal ?? 0);

                if (todosNSeries.Count != esperado)
                    return BadRequest($"Se esperaban {esperado} números de serie para la referencia {dto.Referencia}.");

                if (refData.LongNSerie.HasValue)
                {
                    var longitudEsperada = refData.LongNSerie.Value;
                    foreach (var nserie in todosNSeries)
                    {
                        if (string.IsNullOrEmpty(nserie))
                            return BadRequest("Se proporcionó un número de serie vacío.");
                        if (nserie.Length != longitudEsperada)
                            return BadRequest($"Longitud inválida en número de serie '{nserie}' (esperado: {longitudEsperada}).");
                    }
                }

                for (int i = 0; i < todosNSeries.Count; i++)
                {
                    // Esto para guardar las nserie recepciones
                    var paletAsignado = paletsDeEstaRef[i % paletsDeEstaRef.Count];
                    _context.NSeries_Recepciones.Add(new NSeriesRecepciones
                    {
                        NSerie = todosNSeries[i],
                        Albaran = albaran,
                        Palet = paletAsignado.Palet,
                        Referencia = dto.Referencia,
                        FCreacion = DateTime.Now
                    });
                    // Esto es para los seguimientos
                    _context.NSeries_Seguimiento.Add(new NSeriesSeguimiento
                    {
                        NSerie = todosNSeries[i],
                        Palet = paletAsignado.Palet,
                        Referencia = dto.Referencia,
                        FPicking = DateTime.Now

                    });
                }
            }

            await _context.SaveChangesAsync();

            // 4. Actualizar estado de la cabecera
            cabecera.Estado = 4;
            cabecera.DesEstado = "Confirmado";
            _context.Recepciones_Cab.Update(cabecera);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error en ConfirmarIcp: {ex}");
            return StatusCode(500, "Error interno al confirmar la recepción en ICP.");
        }
    }
}