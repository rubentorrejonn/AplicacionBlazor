using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class RecepcionesCab
    {
        [Column("ALBARAN")]
        public int Albaran { get; set; }
        [Column("PROVEEDOR")]
        public string Proveedor { get; set; } = null!;
        [Column("F_CREACION")]
        public DateTime FCreacion {  get; set; } = DateTime.Now;
        [Column("F_CONFIRMACION")]
        public DateTime FConfirmacion {  get; set; } = DateTime.Now;
        [Column("ESTADO")]
        public int Estado { get; set; }
        [Column("DES_ESTADO")]
        public string DesEstado { get; set; }
    }
}
