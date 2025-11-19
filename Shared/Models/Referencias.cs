using System.ComponentModel.DataAnnotations.Schema;

namespace UltimateProyect.Shared.Models;

public class Referencias
{
    [Column("REFERENCIA")]
    public string Referencia { get; set; } = null!;

    [Column("DES_REFERENCIA")]
    public string DesReferencia { get; set; } = null!;

    [Column("PRECIO")]
    public decimal Precio { get; set; }

    [Column("F_CREACION")]
    public DateTime? FCreacion { get; set; } 

    [Column("NSERIE")]
    public bool? NSerie { get; set; }

    [Column("LONG_NSERIE")]
    public int? LongNSerie { get; set; }

    [Column("OPERATIVO")]
    public bool? Operativo { get; set; }

}