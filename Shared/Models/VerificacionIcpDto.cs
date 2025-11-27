using System.Text.Json.Serialization;

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
    public int Cantidad { get; set; }
    public bool? RequiereNSerie { get; set; }
    public int? LongNSerie { get; set; }
    public List<string> NumerosSerie { get; set; } = new();
    public List<PaletVerificacionDto> PaletsDisponibles { get; set; } = new();
    public List<string> NumerosSerieValidos { get; set; } = new();
    public Dictionary<int, List<string>> NumerosSeriePorPalet { get; set; } = new();
    [JsonIgnore]
    public int Palet => PaletsDisponibles.FirstOrDefault()?.Palet ?? 0;
    [JsonIgnore]
    public string Ubicacion => PaletsDisponibles.FirstOrDefault()?.Ubicacion ?? string.Empty;

}