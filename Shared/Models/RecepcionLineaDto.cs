namespace UltimateProyect.Shared.Models;

public class RecepcionLineaDto
{

    public int Albaran { get; set; }
    public int Linea { get; set; }
    public string Referencia { get; set; } = null!;
    public int? Cantidad { get; set; }
    public int? Bien { get; set; }
    public int? Mal { get; set; }
    public string DesReferencia { get; set; } = string.Empty;
}