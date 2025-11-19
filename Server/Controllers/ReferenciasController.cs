using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateProyect.Server.Data;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReferenciasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReferenciasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Referencias>>> GetReferencias()
    {
        try
        {
            var lista = await _context.Referencias.ToListAsync();
            return Ok(lista);
        }
        catch (Exception ex)
        {
            
            Console.WriteLine("ERROR EN REFERENCIAS:");
            Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
            throw; 
        }
    }

    [HttpGet("{referencia}")]
    public async Task<ActionResult<Referencias>> GetReferencias(string referencia)
    {
        var refObj = await _context.Referencias.FindAsync(referencia);
        if (refObj == null)
            return NotFound("Referencia no encontrada.");
        return refObj;
    }

    [HttpPost]
    public async Task<ActionResult<Referencias>> CreateReferencias(Referencias referencia)
    {
        referencia.FCreacion = DateTime.Now;
        referencia.Operativo = true;

        _context.Referencias.Add(referencia);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReferencias), new { Referencia = referencia.Referencia }, referencia);
    }

    [HttpPut("{referencia}")]
    public async Task<IActionResult> UpdateReferencias(string referencia, Referencias referenciaObj)
    {
        if (referencia != referenciaObj.Referencia)
            return BadRequest("La referencia no coincide.");

        _context.Entry(referenciaObj).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReferenciasExists(referencia))
                return NotFound();
            else
                throw;
        }
        return NoContent();
    }

    [HttpDelete("{referencia}")]
    public async Task<IActionResult> DeleteReferencias(string referencia)
    {
        var refObj = await _context.Referencias.FindAsync(referencia);
        if (refObj == null)
            return NotFound("Referencia no encontrada.");

        _context.Referencias.Remove(refObj);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool ReferenciasExists(string referencia) =>
        _context.Referencias.Any(e => e.Referencia == referencia);
}