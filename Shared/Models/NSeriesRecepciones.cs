using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class NSeriesRecepciones
    {
        //PK
        [Column("NSERIE")]
        public string NSerie { get; set; } = null!;
        //FK
        [Column("ALBARAN")]
        public int Albaran { get; set; }
        //FK
        [Column("PALET")]
        public int Palet { get; set; }
        //FK 
        [Column("REFERENCIA")]
        public string Referencia { get; set; }
        [Column("F_CREACION")]
        public DateTime FCreacion { get; set; }
        [Column("ESTADO")]
        public int Estado { get; set; }
    }
}
