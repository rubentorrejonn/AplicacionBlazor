namespace UltimateProyect.Shared.Models;

public class VerificacionIcpDto
{
    public int Peticion { get; set; }
    public string NombreCliente { get; set; } = null!;
    public List<LineaVerificacionDto> Lineas { get; set; } = new();
}

public class LineaVerificacionDto
{
    public int Linea { get; set; }
    public string Referencia { get; set; } = null!;
    public string DesReferencia { get; set; } = null!;
    public int Palet { get; set; }
    public string Ubicacion { get; set; }
    public int Cantidad { get; set; }
    public bool? RequiereNSerie { get; set; }
    public int? LongNSerie { get; set; }
    public List<string> NumerosSerie { get; set; } = new();
    public List<string> NumerosSerieValidos { get; set; } = new();
}