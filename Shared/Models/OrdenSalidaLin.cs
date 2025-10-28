using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class OrdenSalidaLin
    {
        [Column("PETICION")]
        public int Peticion {  get; set; }
        [Column("LINEA")]
        public int Linea { get; set; }
        [Column("REFERENCIA")]
        public string Referencia { get; set; }
        [Column("CANTIDAD")]
        public int Cantidad { get; set; }
    }
}
