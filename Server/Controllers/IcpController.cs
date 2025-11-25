using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
                    .Where(ns => ns.Referencia == x.lin.Referencia && ns.Estado == 1) // ✅ Solo filtra por Estado = 1
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

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var asignacionesPorLinea = new Dictionary<int, List<Palets>>();

            foreach (var linea in verificacion.Lineas)
            {
                var cantidadNecesaria = linea.Cantidad;
                var paletsDisponibles = await _context.Palets
                    .Where(p => p.Referencia == linea.Referencia && p.Estado == 3)
                    .OrderBy(p => p.Palet)
                    .ToListAsync();

                var paletsAsignadosALinea = new List<Palets>();
                var cantidadesPorPalet = new List<int>();

                foreach (var palet in paletsDisponibles)
                {
                    if (cantidadNecesaria <= 0) break;

                    var cantidadARestar = Math.Min(cantidadNecesaria, palet.Cantidad);
                    palet.Cantidad -= cantidadARestar;
                    cantidadNecesaria -= cantidadARestar;

                    paletsAsignadosALinea.Add(palet);
                    cantidadesPorPalet.Add(cantidadARestar);

                    if (palet.Cantidad == 0)
                        palet.Estado = 5;
                    else
                        palet.Estado = 1;

                    _context.Palets.Update(palet);
                }

                if (cantidadNecesaria > 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"No hay suficiente stock para la referencia {linea.Referencia}.");
                }

                asignacionesPorLinea[linea.Linea] = paletsAsignadosALinea;

                if (linea.RequiereNSerie == true)
                {
                    var nsDisponibles = await _context.NSeries_Recepciones
                        .Where(ns => ns.Referencia == linea.Referencia && ns.Estado == 1)
                        .Select(ns => ns.NSerie)
                        .ToListAsync();

                    var numerosSerieValidos = linea.NumerosSerie
                        .Where(ns => !string.IsNullOrWhiteSpace(ns))
                        .ToList();

                    if (numerosSerieValidos.Count != linea.Cantidad)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"La línea {linea.Linea} requiere {linea.Cantidad} números de serie, pero se proporcionaron {numerosSerieValidos.Count}.");
                    }

                    var nsInvalidos = numerosSerieValidos.Except(nsDisponibles, StringComparer.OrdinalIgnoreCase).ToList();
                    if (nsInvalidos.Any())
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Números de serie inválidos: {string.Join(", ", nsInvalidos)}");
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
                                _context.NSeries_Recepciones.Update(nsEntity);
                            }
                        }
                    }
                }

                var usuario = await _context.Usuarios.FindAsync(userIdActual);
                if (usuario == null)
                    return Unauthorized("Usuario no encontrado.");

                for (int i = 0; i < paletsAsignadosALinea.Count; i++)
                {
                    var log = new PickingLogs
                    {
                        Peticion = verificacion.Peticion,
                        Palet = paletsAsignadosALinea[i].Palet,
                        Referencia = paletsAsignadosALinea[i].Referencia,
                        CantidadQuitada = cantidadesPorPalet[i],
                        FechaVerificacion = DateTime.Now,
                        IdUsuario = userIdActual,
                        NombreUsuario = usuario.UserName
                    };
                    _context.PickingLog.Add(log);
                }
            }

            cabecera.Estado = 3;
            _context.Orden_Salida_Cab.Update(cabecera);

            // ✅ Ejecutar PA solo para líneas que requieren NSerie
            foreach (var linea in verificacion.Lineas)
            {
                if (linea.RequiereNSerie == true)
                {
                    var paletsDeEstaLinea = asignacionesPorLinea.GetValueOrDefault(linea.Linea, new List<Palets>());
                    if (!paletsDeEstaLinea.Any()) continue;

                    var paletAsignado = paletsDeEstaLinea.First();

                    foreach (var nserie in linea.NumerosSerie)
                    {
                        if (string.IsNullOrEmpty(nserie)) continue;

                        var connectionString = _context.Database.GetConnectionString();
                        using var connection = new SqlConnection(connectionString);
                        await connection.OpenAsync();

                        using var command = new SqlCommand("PA_NSERIES_SEGUIMIENTO", connection);
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add(new SqlParameter("@NSERIE", SqlDbType.VarChar, 30) { Value = nserie });
                        command.Parameters.Add(new SqlParameter("@PALET", SqlDbType.Int) { Value = paletAsignado.Palet });
                        command.Parameters.Add(new SqlParameter("@ALBARAN", SqlDbType.Int) { Value = verificacion.Peticion });
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
                }
            }

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