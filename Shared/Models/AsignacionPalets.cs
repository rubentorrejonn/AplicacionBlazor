using System.ComponentModel.DataAnnotations.Schema;

public class AsignacionPalets
{
    [Column("ID")]
    public int Id { get; set; }
    [Column("PALET")]
    public int Palet { get; set; }
    [Column("PETICION")]
    public int? Peticion { get; set; }
    [Column("LINEA")]
    public int? Linea { get; set; }
    [Column("FECHA_ASIGNACION")]
    public DateTime FechaAsignacion { get; set; }
}