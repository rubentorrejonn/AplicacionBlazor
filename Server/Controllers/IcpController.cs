using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
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
                  (lin, refe) => new { lin, refe })
            .Select(x => new LineaVerificacionDto
            {
                Linea = x.lin.Linea,
                Referencia = x.lin.Referencia,
                DesReferencia = x.refe.DesReferencia,
                Cantidad = x.lin.Cantidad,
                RequiereNSerie = x.refe.NSerie,
                LongNSerie = x.refe.LongNSerie,
                Palet = 0,
                Ubicacion = string.Empty,
                NumerosSerieValidos = _context.NSeries_Recepciones
                    .Where(ns => ns.Referencia == x.lin.Referencia && ns.Estado == 1)
                    .Select(ns => ns.NSerie)
                    .ToList()
            })
            .OrderBy(l => l.Linea)
            .ToListAsync();

        foreach (var linea in lineas)
        {
            var paletAsociado = await _context.Palets
                .Where(p => p.Referencia == linea.Referencia && p.Estado == 3)
                .OrderBy(p => p.Palet)
                .FirstOrDefaultAsync();

            if (paletAsociado != null)
            {
                linea.Palet = paletAsociado.Palet;
                linea.Ubicacion = paletAsociado.Ubicacion;
            }
        }

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

        using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
        var connection = _context.Database.GetDbConnection();
        var dbTransaction = transaction.GetDbTransaction();


        try
        {
            foreach (var linea in verificacion.Lineas)
            {
                var paletsDisponibles = await _context.Palets
                    .Where(p => p.Referencia == linea.Referencia && p.Estado == 3)
                    .OrderBy(p => p.Palet)
                    .ToListAsync();

                var cantidadNecesaria = linea.Cantidad;
                foreach (var palet in paletsDisponibles)
                {
                    if (cantidadNecesaria <= 0) break;
                    cantidadNecesaria -= Math.Min(cantidadNecesaria, palet.Cantidad);
                }

                if (cantidadNecesaria > 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"No hay suficiente stock para la referencia {linea.Referencia}.");
                }

                if (linea.RequiereNSerie == true)
                {
                    var numerosSerieValidos = linea.NumerosSerie
                        .Where(ns => !string.IsNullOrWhiteSpace(ns))
                        .ToList();

                    if (numerosSerieValidos.Count != linea.Cantidad)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"La línea {linea.Linea} requiere {linea.Cantidad} números de serie, pero se proporcionaron {numerosSerieValidos.Count}.");
                    }

                    var nsDisponiblesSet = new HashSet<string>(
                        await _context.NSeries_Recepciones
                            .Where(ns => ns.Referencia == linea.Referencia && ns.Estado == 1)
                            .Select(ns => ns.NSerie)
                            .ToListAsync(),
                        StringComparer.OrdinalIgnoreCase
                    );

                    var todosValidos = numerosSerieValidos.All(ns => nsDisponiblesSet.Contains(ns));
                    if (!todosValidos)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest("Números de serie inválidos.");
                    }
                }
            }

            var todosLosLogs = new List<PickingLogs>();
            foreach (var linea in verificacion.Lineas)
            {
                var paletsDisponibles = await _context.Palets
                    .Where(p => p.Referencia == linea.Referencia && p.Estado == 3)
                    .OrderBy(p => p.Palet)
                    .ToListAsync();

                var cantidadNecesaria = linea.Cantidad;
                foreach (var palet in paletsDisponibles)
                {
                    if (cantidadNecesaria <= 0) break;

                    var cantidadARestar = Math.Min(cantidadNecesaria, palet.Cantidad);
                    palet.Cantidad -= cantidadARestar;
                    cantidadNecesaria -= cantidadARestar;

                    palet.Estado = (palet.Cantidad == 0) ? 5 : 1;
                }

                var paletOriginal = await _context.Palets
                    .Where(p => p.Referencia == linea.Referencia && p.Estado == 3)
                    .OrderBy(p => p.Palet)
                    .FirstOrDefaultAsync();

                if (paletOriginal == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"No hay palet disponible para la referencia {linea.Referencia}.");
                }

                var nuevoPalet = new Palets
                {
                    Referencia = linea.Referencia,
                    Cantidad = linea.Cantidad,
                    Albaran = paletOriginal.Albaran,
                    Ubicacion = paletOriginal.Ubicacion,
                    Estado = 4,
                    FInsert = DateTime.Now
                };

                _context.Palets.Add(nuevoPalet);

                await _context.SaveChangesAsync();


                if (linea.RequiereNSerie == true && linea.NumerosSerie != null)
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    foreach (var nserie in linea.NumerosSerie)
                    {
                        if (string.IsNullOrEmpty(nserie)) continue;

                        using var command = connection.CreateCommand();
                        command.CommandText = "PA_NSERIES_SEGUIMIENTO";
                        command.CommandType = CommandType.StoredProcedure;
                        command.Transaction = dbTransaction;

                        command.Parameters.Add(new SqlParameter("@NSERIE", SqlDbType.VarChar, 30) { Value = nserie });
                        command.Parameters.Add(new SqlParameter("@PALET", SqlDbType.Int) { Value = nuevoPalet.Palet });
                        command.Parameters.Add(new SqlParameter("@PETICION", SqlDbType.Int) { Value = verificacion.Peticion });
                        command.Parameters.Add(new SqlParameter("@ALBARAN", SqlDbType.Int) { Value = paletOriginal.Albaran});
                        command.Parameters.Add(new SqlParameter("@REFERENCIA", SqlDbType.VarChar, 30) { Value = linea.Referencia });
                        command.Parameters.Add(new SqlParameter("@INVOKER", SqlDbType.Int) { Value = 0 });
                        command.Parameters.Add(new SqlParameter("@USUARIO", SqlDbType.VarChar, 12) { Value = "" });
                        command.Parameters.Add(new SqlParameter("@CULTURA", SqlDbType.VarChar, 5) { Value = "" });

                        var retCodeParam = new SqlParameter("@RETCODE", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };
                        command.Parameters.Add(retCodeParam);
                        command.Parameters.Add(mensajeParam);

                        await command.ExecuteNonQueryAsync();

                        int retCode = retCodeParam.Value is int r ? r : -1;
                        if (retCode != 0)
                        {
                            var mensaje = mensajeParam.Value?.ToString() ?? "Error desconocido en PA_NSERIES_SEGUIMIENTO";
                            await transaction.RollbackAsync();
                            return BadRequest($"Error en el seguimiento de NSerie '{nserie}': {mensaje}");
                        }
                    }

                    foreach (var nserie in linea.NumerosSerie)
                    {
                        if (!string.IsNullOrEmpty(nserie))
                        {
                            var nsEntity = await _context.NSeries_Recepciones
                                .FirstOrDefaultAsync(ns => ns.NSerie == nserie && ns.Referencia == linea.Referencia);
                            if (nsEntity != null)
                            {
                                nsEntity.Estado = 0;
                            }
                        }
                    }
                }

                var usuario = await _context.Usuarios.FindAsync(userIdActual);
                var log = new PickingLogs
                {
                    Peticion = verificacion.Peticion,
                    Palet = nuevoPalet.Palet,
                    PaletRetirada = paletOriginal.Palet,
                    Referencia = linea.Referencia,
                    CantidadQuitada = linea.Cantidad,
                    FechaVerificacion = DateTime.Now,
                    IdUsuario = userIdActual,
                    NombreUsuario = usuario?.UserName ?? ""
                };
                todosLosLogs.Add(log);

                var movimiento = await _context.Movimientos
                    .FirstOrDefaultAsync(m =>
                        m.Peticion == verificacion.Peticion &&
                        m.Referencia == linea.Referencia &&
                        m.LinPeticion == linea.Linea);

                if (movimiento != null)
                {
                    movimiento.Palet = nuevoPalet.Palet;
                    movimiento.UbicacionDestino = "TRANSPORTE";
                    movimiento.Realizado = 1;
                }
            }

            _context.PickingLog.AddRange(todosLosLogs);
            cabecera.Estado = 3;

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

    [HttpGet("log")]
    public async Task<ActionResult<List<PickingLogs>>> GetAllPickingLogs()
    {
        var logs = await _context.PickingLog
            .Join(_context.Usuarios,
                  log => log.IdUsuario,
                  user => user.IdUsuario,
                  (log, user) => new PickingLogs
                  {
                      Id = log.Id,
                      Peticion = log.Peticion,
                      Palet = log.Palet,
                      PaletRetirada = log.PaletRetirada,
                      Referencia = log.Referencia,
                      CantidadQuitada = log.CantidadQuitada,
                      FechaVerificacion = log.FechaVerificacion,
                      IdUsuario = log.IdUsuario,
                      NombreUsuario = user.UserName
                  })
            .OrderByDescending(l => l.FechaVerificacion)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("log/{peticion}")]
    public async Task<ActionResult<List<PickingLogs>>> GetPickingLog(int peticion)
    {
        var logs = await _context.PickingLog
            .Where(l => l.Peticion == peticion)
            .Join(_context.Usuarios,
                  log => log.IdUsuario,
                  usuario => usuario.IdUsuario,
                  (log, usuario) => new PickingLogs
                  {
                      Id = log.Id,
                      Peticion = log.Peticion,
                      Palet = log.Palet,
                      PaletRetirada = log.PaletRetirada,
                      Referencia = log.Referencia,
                      CantidadQuitada = log.CantidadQuitada,
                      FechaVerificacion = log.FechaVerificacion,
                      IdUsuario = log.IdUsuario,
                      NombreUsuario = usuario.UserName
                  })
            .OrderBy(l => l.FechaVerificacion)
            .ToListAsync();
        return Ok(logs);
    }
}