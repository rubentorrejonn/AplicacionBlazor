using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdenSalidaCabController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdenSalidaCabController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrdenSalidaCab>>> GetOrdenSalidaCab()
    {
        return await _context.Orden_Salida_Cab.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrdenSalidaCab>> GetOrdenSalidaCab(int id)
    {
        var cab = await _context.Orden_Salida_Cab.FindAsync(id);
        if (cab == null) return NotFound("Cabecera no encontrada");
        return cab;
    }

    [HttpPost]
    public async Task<ActionResult<OrdenSalidaCab>> PostOrdenSalida(OrdenSalidaCab cab)
    {
        _context.Orden_Salida_Cab.Add(cab);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOrdenSalidaCab), new { id = cab.Peticion }, cab);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrdenSalida(int id, OrdenSalidaCab cab)
    {
        if (id != cab.Peticion) return BadRequest();
        _context.Entry(cab).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrdenSalidaCabExists(id))
                return NotFound();
            else
                throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cab = await _context.Orden_Salida_Cab.FindAsync(id);
        if (cab == null) return NotFound();
        _context.Orden_Salida_Cab.Remove(cab);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool OrdenSalidaCabExists(int id) =>
        _context.Orden_Salida_Cab.Any(e => e.Peticion == id);
}