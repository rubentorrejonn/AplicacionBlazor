using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecepcionesCabController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RecepcionesCabController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecepcionesCab>>> GetRecepcionesCab()
    {
        return await _context.Recepciones_Cab.ToListAsync();
    }

    [HttpGet("log")]
    public async Task<ActionResult<List<VistaOrdenSalidaCab>>> GetOrdenSalidaCab()
    {
        var cabeceras = await _context.V_RECEPCIONES_LOG.ToListAsync();
        return Ok(cabeceras);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecepcionesCab>> GetRecepcionesCab(int id)
    {
        var cabecera = await _context.Recepciones_Cab.FindAsync(id);
        if (cabecera == null)
            return NotFound("Cabecera no encontrada.");
        return cabecera;
    }

    [HttpPost]
    public async Task<ActionResult<RecepcionesCab>> PostRecepcion(RecepcionesCab recepcion)
    {
        // No cambiar Estado ni DesEstado aquí
        _context.Recepciones_Cab.Add(recepcion);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetRecepcion", new { id = recepcion.Albaran }, recepcion);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecepcionesCab(int id, RecepcionesCab recepcionCab)
    {
        if (id != recepcionCab.Albaran)
            return BadRequest("La referencia no coincide.");

        _context.Entry(recepcionCab).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RecepcionesCabExists(id))
                return NotFound();
            else
                throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecepcionesCab(int id)
    {
        var cabecera = await _context.Recepciones_Cab.FindAsync(id);
        if (cabecera == null)
            return NotFound("Cabecera no encontrada.");

        _context.Recepciones_Cab.Remove(cabecera);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool RecepcionesCabExists(int id) =>
        _context.Recepciones_Cab.Any(e => e.Albaran == id);
}