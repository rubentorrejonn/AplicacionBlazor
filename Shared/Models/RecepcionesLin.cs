using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class RecepcionesLin
    {
        [Column("ALBARAN")]
        public int Albaran {  get; set; }
        [Column("LINEA")]
        public int Linea { get; set; }
        [Column("REFERENCIA")]
        public string Referencia { get; set; } = null!;
        [Column("CANTIDAD")]
        public int Cantidad { get; set; }
        [Column("BIEN")]
        public int Bien { get; set; }
        [Column("MAL")]
        public int Mal { get; set; }
    }
}
