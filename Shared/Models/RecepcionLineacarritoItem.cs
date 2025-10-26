namespace UltimateProyect.Shared.Models;

public class RecepcionLineaCarritoItem
{
    public string Referencia { get; set; } = string.Empty;
    public string DesReferencia { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public bool Mal { get; set; }
}