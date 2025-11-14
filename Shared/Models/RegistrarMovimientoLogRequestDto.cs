public class RegistrarMovimientoLogRequestDto
{
    public string? UbicacionOrigen { get; set; }
    public string? UbicacionDestino { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public int IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public int Peticion { get; set; }
    public int Palet { get; set; }
    public string? Referencia { get; set; }
}