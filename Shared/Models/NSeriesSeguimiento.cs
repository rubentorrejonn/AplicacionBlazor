using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class NSeriesSeguimiento
    {
        //PK y FK DE NSERIE_RECEPCIONES
        [Column("NSERIE")]
        public string NSerie { get; set; } = null!;
        [Column("PETICION")]
        public int? Peticion { get; set; }
        //FK
        [Column("PALET")]
        public int Palet { get; set; }
        //FK
        [Column("REFERENCIA")]
        public string Referencia { get; set; } = null!;
        [Column("F_PICKING")]
        public DateTime FPicking { get; set; } = DateTime.Now;

    }
}
