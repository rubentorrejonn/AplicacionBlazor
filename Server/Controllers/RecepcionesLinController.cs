using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
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
        var cabecera = await _context.Recepciones_Cab.FindAsync(albaran);
        if (cabecera == null)
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
            var paletsExistentes = await _context.Palets
                .Where(p => p.Albaran == albaran)
                .Select(p => p.Palet)
                .ToListAsync();

            if (paletsExistentes.Any())
            {
                var nseriesExistentes = await _context.NSeries_Recepciones
                    .Where(ns => paletsExistentes.Contains(ns.Palet))
                    .ToListAsync();
                _context.NSeries_Recepciones.RemoveRange(nseriesExistentes);
                /*
                var seguimientosExistentes = await _context.NSeries_Seguimiento
                    .Where(ns => paletsExistentes.Contains(ns.Palet))
                    .ToListAsync();
                _context.NSeries_Seguimiento.RemoveRange(seguimientosExistentes);
                */
                var paletsToRemove = await _context.Palets
                    .Where(p => p.Albaran == albaran)
                    .ToListAsync();
                _context.Palets.RemoveRange(paletsToRemove);
            }

            var lineasExistentes = await _context.Recepciones_Lin
                .Where(l => l.Albaran == albaran)
                .ToListAsync();
            _context.Recepciones_Lin.RemoveRange(lineasExistentes);

            await _context.SaveChangesAsync();

            var entidadesLin = lineasDto.Select((dto, index) => new RecepcionesLin
            {
                Albaran = dto.Albaran,
                Linea = index + 1,
                Referencia = dto.Referencia,
                Cantidad = dto.Cantidad ?? 0
            }).ToList();

            _context.Recepciones_Lin.AddRange(entidadesLin);
            await _context.SaveChangesAsync();

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
                        Ubicacion = "UBI-5",
                        Estado = 1,
                        FInsert = DateTime.Now
                    });
                    unidades -= cant;
                }
            }

            _context.Palets.AddRange(palets);
            await _context.SaveChangesAsync();

            if (cabecera.Estado == 1)
            {
                cabecera.Estado = 2;
                cabecera.DesEstado = "Recepcionado";
                _context.Recepciones_Cab.Update(cabecera);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error al crear/editar recepción: {ex}");
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
        var ubicacion = request.Ubicacion ?? "UBI-5";

        if (lineasDto == null || !lineasDto.Any())
            return BadRequest("No se proporcionaron líneas.");

        var cabecera = await _context.Recepciones_Cab.FindAsync(albaran);
        if (cabecera == null)
            return NotFound("Albarán no encontrado.");

        var fechaConfirmacion = DateTime.Now;
        cabecera.FConfirmacion = fechaConfirmacion;

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

            var paletsDelAlbaran = await _context.Palets
                .Where(p => p.Albaran == albaran)
                .Select(p => p.Palet)
            .ToListAsync();

            if (paletsDelAlbaran.Any())
            {
                // Eliminamos NSeries_Recepciones que apunten a esos palets
                var nseriesExistentes = await _context.NSeries_Recepciones
                    .Where(ns => paletsDelAlbaran.Contains(ns.Palet))
                    .ToListAsync();
                _context.NSeries_Recepciones.RemoveRange(nseriesExistentes);

                // Si también gestionas NSeries_Seguimiento aquí,
                /*
                var seguimientosExistentes = await _context.NSeries_Seguimiento
                    .Where(ns => paletsDelAlbaran.Contains(ns.Palet))
                    .ToListAsync();
                _context.NSeries_Seguimiento.RemoveRange(seguimientosExistentes);
                */
            }

            var paletsAnteriores = await _context.Palets
             .Where(p => p.Albaran == albaran)
                 .ToListAsync();
            _context.Palets.RemoveRange(paletsAnteriores);

            await _context.SaveChangesAsync(); // Guardamos la eliminación de NSeries y Palets

            //Crear nuevos palets según Bien/Mal
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
                            Ubicacion = "UBI-5",
                            Estado = 1,
                            FInsert = fechaConfirmacion
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
                            FInsert = fechaConfirmacion
                        });
                        unidades -= cant;
                    }
                }
            }

            _context.Palets.AddRange(nuevosPalets);
            await _context.SaveChangesAsync(); // Guardamos nuevos palets

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
                    var paletAsignado = paletsDeEstaRef[i % paletsDeEstaRef.Count];
                    _context.NSeries_Recepciones.Add(new NSeriesRecepciones
                    {
                        NSerie = todosNSeries[i],
                        Albaran = albaran,
                        Palet = paletAsignado.Palet,
                        Referencia = dto.Referencia,
                        FCreacion = DateTime.Now
                    });
                    // Lo reutilizare en otro sitio
                    /*_context.NSeries_Seguimiento.Add(new NSeriesSeguimiento
                    {
                        NSerie = todosNSeries[i],
                        Palet = paletAsignado.Palet,
                        Referencia = dto.Referencia,
                        FPicking = DateTime.Now
                    });*/
                }
            }

            await _context.SaveChangesAsync(); // Guardamos nuevos NSeries

            var referenciasIds = request.Lineas.Select(l => l.Referencia).Distinct().ToList();
            var referenciasDict = await _context.Referencias
                .Where(r => referenciasIds.Contains(r.Referencia))
                .ToDictionaryAsync(r => r.Referencia, r => r.DesReferencia);

            var cuerpoEmail = new StringBuilder();
            cuerpoEmail.AppendLine($"Confirmación de recepcion - Albarán {albaran}");
            cuerpoEmail.AppendLine("==========================================");
            cuerpoEmail.AppendLine("Referencia\tDescripcion\t\tCantidad Bien");
            cuerpoEmail.AppendLine("----------------------------------------------------------------");
            var asuntoEmail = $"Confirmación ICP - Albarán {albaran} - Prueba {Guid.NewGuid().ToString().Substring(0, 5)}";

            foreach (var linea in request.Lineas)
            {
                cuerpoEmail.AppendLine($"{linea.Referencia}\t\t{linea.DesReferencia}\t\t\t{linea.Bien ?? 0}");
            }

            // Actualizar estado de la cabecera
            cabecera.FConfirmacion = fechaConfirmacion;
            cabecera.Estado = 4;
            cabecera.DesEstado = "Confirmado";
            _context.Recepciones_Cab.Update(cabecera);

             await _context.SaveChangesAsync(); // Guardamos el cambio de estado
            await transaction.CommitAsync();
            try
            {
                var connectionString = _context.Database.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("PA_RECEPCIONES_Y_DBMAIL", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        

                        command.Parameters.Add(new SqlParameter("@ALBARAN", SqlDbType.Int) { Value = albaran });
                        command.Parameters.Add(new SqlParameter("@INVOKER", SqlDbType.Int) { Value = 0 });
                        command.Parameters.Add(new SqlParameter("@USUARIO", SqlDbType.VarChar, 12) { Value = "" });
                        command.Parameters.Add(new SqlParameter("@CULTURA", SqlDbType.VarChar, 5) { Value = "" });


                        var retCodeParam = new SqlParameter("@RETCODE", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };
                        command.Parameters.Add(retCodeParam);
                        command.Parameters.Add(mensajeParam);

                        await command.ExecuteNonQueryAsync();

                        int retCode = (int)retCodeParam.Value;
                        Console.WriteLine($"[DEBUG] RETCODE recibido: {retCode}");

                        if (retCode != 0)
                        {
                            Console.WriteLine($"[ERROR] PA_ENVIAR_DBMAIL falló con RETCODE: {retCode}");
                        }
                        else
                        {
                            Console.WriteLine("[INFO] PA_ENVIAR_DBMAIL ejecutado correctamente (RETCODE: 0).");
                        }
                    }
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                Console.WriteLine("");
                Console.WriteLine("=== [C# ERROR DETALLADO SQL - PA_ENVIAR_DBMAIL] ===");
                Console.WriteLine($"Mensaje: {sqlEx.Message}");
                Console.WriteLine($"Número de Error SQL: {sqlEx.Number}");
                Console.WriteLine($"Procedimiento: {sqlEx.Procedure}");
                Console.WriteLine($"Línea: {sqlEx.LineNumber}");
                Console.WriteLine("StackTrace (parcial):");
                Console.WriteLine(sqlEx.StackTrace.Substring(0, Math.Min(500, sqlEx.StackTrace.Length)));
                Console.WriteLine("=== [FIN ERROR SQL] ===");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("=== [C# ERROR GENERAL - PA_ENVIAR_DBMAIL] ===");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine("StackTrace (parcial):");
                Console.WriteLine(ex.StackTrace.Substring(0, Math.Min(500, ex.StackTrace.Length)));
                Console.WriteLine("=== [FIN ERROR GENERAL] ===");
                Console.WriteLine("");
            }

            // Cierra el try principal aquí
            return NoContent();
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                Console.WriteLine($"Error al hacer rollback: {rollbackEx}");
            }
            Console.WriteLine($"Error en ConfirmarIcp: {ex}");
            return StatusCode(500, "Error interno al confirmar la recepción en ICP.");
        }

    }
    [HttpPost("asignar-stock/{peticion}")]
    public async Task<IActionResult> AsignarStock(int peticion)
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("PA_ASIGNAR_STOCK", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parámetros de entrada
                    command.Parameters.Add(new SqlParameter("@PETICION", SqlDbType.Int) { Value = peticion });
                    command.Parameters.Add(new SqlParameter("@INVOKER", SqlDbType.Int) { Value = 0 }); 
                    command.Parameters.Add(new SqlParameter("@USUARIO", SqlDbType.VarChar, 12) { Value = ""});
                    command.Parameters.Add(new SqlParameter("@CULTURA", SqlDbType.VarChar, 5) { Value = "" });

                    // Parámetros de salida
                    var retCodeParam = new SqlParameter("@RETCODE", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };

                    command.Parameters.Add(retCodeParam);
                    command.Parameters.Add(mensajeParam);

                    await command.ExecuteNonQueryAsync();

                    int retCode = (int)retCodeParam.Value;
                    string mensaje = mensajeParam.Value as string ?? "";

                    if (retCode != 0)
                    {
                        return BadRequest(new { Message = mensaje });
                    }

                    return Ok(new { Message = mensaje });
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error al asignar stock: {ex.Message}" });
        }
    }
}