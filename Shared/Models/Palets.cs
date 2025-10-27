using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class Palets
    {
        //PK
        [Column("PALET")]
        public int Palet { get; set; }
        //FK
        [Column("REFERENCIA")]
        public string Referencia { get; set; }
        [Column("CANTIDAD")]
        public int Cantidad { get; set; }
        [Column("ALBARAN")]
        public int Albaran { get; set; }
        //FK
        [Column("UBICACION")]
        public string Ubicacion { get; set; }
        [Column("ESTADO")]
        public int Estado { get; set; }
        [Column("F_INSERT")]
        public DateTime FInsert { get; set; }
    }
}
