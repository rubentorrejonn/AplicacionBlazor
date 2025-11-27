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

        var lineasConPalets = await _context.Orden_Salida_Lin
            .Where(l => l.Peticion == peticion)
            .Join(_context.Referencias,
                  lin => lin.Referencia,
                  refe => refe.Referencia,
                  (lin, refe) => new { lin, refe })
            .ToListAsync();

        var resultado = new List<LineaVerificacionDto>();
        foreach (var item in lineasConPalets)
        {
            var nsGlobal = await _context.NSeries_Recepciones
                .Where(ns => ns.Referencia == item.lin.Referencia && ns.Estado == 1)
                .Select(ns => ns.NSerie)
                .ToListAsync();

            var paletsAsignados = await _context.PeticionPalets
                .Where(pp => pp.Peticion == peticion)
                .Join(_context.Palets,
                      pp => pp.Palet,
                      p => p.Palet,
                      (pp, p) => new { pp, p })
                .Where(x => x.p.Referencia == item.lin.Referencia)
                .Select(x => new PaletVerificacionDto
                {
                    Palet = x.p.Palet,
                    Ubicacion = x.p.Ubicacion,
                    Cantidad = x.pp.Cantidad,
                    NumerosSerieValidos = _context.NSeries_Recepciones
                        .Where(ns => ns.Palet == x.p.Palet && ns.Estado == 1)
                        .Select(ns => ns.NSerie)
                        .ToList()
                })
                .OrderBy(p => p.Palet)
                .ToListAsync();

            resultado.Add(new LineaVerificacionDto
            {
                Linea = item.lin.Linea,
                Referencia = item.lin.Referencia,
                DesReferencia = item.refe.DesReferencia,
                Cantidad = item.lin.Cantidad,
                RequiereNSerie = item.refe.NSerie,
                LongNSerie = item.refe.LongNSerie,
                NumerosSerieValidos = nsGlobal,
                PaletsDisponibles = paletsAsignados
            });
        }

        var verificacion = new VerificacionIcpDto
        {
            Peticion = peticion,
            NombreCliente = cabecera.NombreCliente,
            Lineas = resultado
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

        try
        {
            foreach (var linea in verificacion.Lineas)
            {
                var paletsAsignados = await _context.PeticionPalets
                    .Where(pp => pp.Peticion == verificacion.Peticion && pp.Referencia == linea.Referencia)
                    .ToListAsync();

                var totalAsignado = paletsAsignados.Sum(p => p.Cantidad);
                if (totalAsignado != linea.Cantidad)
                    return BadRequest($"La cantidad asignada ({totalAsignado}) no coincide con la línea ({linea.Cantidad}).");

                if (linea.RequiereNSerie == true)
                {
                    var nsIngresados = linea.NumerosSerie
                        .Where(ns => !string.IsNullOrWhiteSpace(ns))
                        .ToList();

                    if (nsIngresados.Count != linea.Cantidad)
                        return BadRequest($"La línea {linea.Linea} requiere {linea.Cantidad} números de serie.");

                    var indice = 0;
                    foreach (var paletAsig in paletsAsignados.OrderBy(p => p.Palet))
                    {
                        var nsParaPalet = nsIngresados.Skip(indice).Take(paletAsig.Cantidad).ToList();
                        indice += paletAsig.Cantidad;

                        var nsValidos = await _context.NSeries_Recepciones
                            .Where(ns => ns.Palet == paletAsig.Palet && ns.Estado == 1)
                            .Select(ns => ns.NSerie)
                            .ToListAsync();

                        var invalidos = nsParaPalet.Except(nsValidos, StringComparer.OrdinalIgnoreCase).ToList();
                        if (invalidos.Any())
                            return BadRequest($"NSeries inválidos para el palet {paletAsig.Palet}: {string.Join(", ", invalidos)}");
                    }
                }
            }

            var todosLosLogs = new List<PickingLogs>();
            foreach (var linea in verificacion.Lineas)
            {
                var paletsAsignados = await _context.PeticionPalets
                    .Where(pp => pp.Peticion == verificacion.Peticion && pp.Referencia == linea.Referencia)
                    .OrderBy(pp => pp.Palet)
                    .ToListAsync();

                foreach (var paletAsig in paletsAsignados)
                {
                    var paletOriginal = await _context.Palets.FindAsync(paletAsig.Palet);
                    if (paletOriginal == null) continue;

                    paletOriginal.Cantidad -= paletAsig.Cantidad;
                    paletOriginal.Estado = paletOriginal.Cantidad == 0 ? 5 : 1;
                    _context.Palets.Update(paletOriginal);

                    var nuevoPalet = new Palets
                    {
                        Referencia = linea.Referencia,
                        Cantidad = paletAsig.Cantidad,
                        Albaran = paletOriginal.Albaran,
                        Ubicacion = paletOriginal.Ubicacion,
                        Estado = 4,
                        FInsert = DateTime.Now
                    };
                    _context.Palets.Add(nuevoPalet);
                }
            }

            await _context.SaveChangesAsync();

            foreach (var linea in verificacion.Lineas)
            {
                var paletsAsignados = await _context.PeticionPalets
                    .Where(pp => pp.Peticion == verificacion.Peticion && pp.Referencia == linea.Referencia)
                    .OrderBy(pp => pp.Palet)
                    .ToListAsync();

                var nsIngresados = linea.NumerosSerie.Where(ns => !string.IsNullOrWhiteSpace(ns)).ToList();
                int indiceNs = 0;

                foreach (var paletAsig in paletsAsignados)
                {
                    var paletOriginal = await _context.Palets.FindAsync(paletAsig.Palet);
                    if (paletOriginal == null) continue;

                    var nuevoPalet = await _context.Palets
                        .Where(p => p.Referencia == linea.Referencia && p.Estado == 4 && p.Albaran == paletOriginal.Albaran)
                        .OrderByDescending(p => p.Palet)
                        .FirstOrDefaultAsync();

                    if (nuevoPalet == null) continue;

                    var nsParaPalet = nsIngresados.Skip(indiceNs).Take(paletAsig.Cantidad).ToList();
                    indiceNs += paletAsig.Cantidad;

                    if (linea.RequiereNSerie == true)
                    {
                        foreach (var nserie in nsParaPalet)
                        {
                            var connectionString = _context.Database.GetConnectionString();
                            using var connection = new SqlConnection(connectionString);
                            await connection.OpenAsync();
                            using var command = new SqlCommand("PA_NSERIES_SEGUIMIENTO", connection);
                            command.CommandType = CommandType.StoredProcedure;

                            command.Parameters.Add(new SqlParameter("@NSERIE", SqlDbType.VarChar, 30) { Value = nserie });
                            command.Parameters.Add(new SqlParameter("@PALET", SqlDbType.Int) { Value = nuevoPalet.Palet });
                            command.Parameters.Add(new SqlParameter("@PETICION", SqlDbType.Int) { Value = verificacion.Peticion });
                            command.Parameters.Add(new SqlParameter("@ALBARAN", SqlDbType.Int) { Value = paletOriginal.Albaran });
                            command.Parameters.Add(new SqlParameter("@REFERENCIA", SqlDbType.VarChar, 30) { Value = linea.Referencia });
                            command.Parameters.Add(new SqlParameter("@INVOKER", SqlDbType.Int) { Value = 0 });
                            command.Parameters.Add(new SqlParameter("@USUARIO", SqlDbType.VarChar, 12) { Value = "" });
                            command.Parameters.Add(new SqlParameter("@CULTURA", SqlDbType.VarChar, 5) { Value = "" });

                            var retCodeParam = new SqlParameter("@RETCODE", SqlDbType.Int) { Direction = ParameterDirection.Output };
                            var mensajeParam = new SqlParameter("@MENSAJE", SqlDbType.VarChar, 1000) { Direction = ParameterDirection.Output };
                            command.Parameters.Add(retCodeParam);
                            command.Parameters.Add(mensajeParam);

                            await command.ExecuteNonQueryAsync();

                            var nsEntity = await _context.NSeries_Recepciones
                                .FirstOrDefaultAsync(ns => ns.NSerie == nserie && ns.Palet == paletAsig.Palet);
                            if (nsEntity != null)
                            {
                                nsEntity.Estado = 0;
                                _context.NSeries_Recepciones.Update(nsEntity);
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
                        CantidadQuitada = paletAsig.Cantidad,
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
                        _context.Movimientos.Update(movimiento);
                    }
                }
            }

            _context.PickingLog.AddRange(todosLosLogs);
            cabecera.Estado = 3;
            _context.Orden_Salida_Cab.Update(cabecera);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Verificación completada correctamente." });
        }
        catch (Exception ex)
        {
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