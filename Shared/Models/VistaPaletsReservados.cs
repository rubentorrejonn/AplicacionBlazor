using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class VistaPaletsReservados
    {
        [Column("PALET")]
        public int Palet { get; set; }
        [Column("CANTIDAD")]
        public int Cantidad { get; set; }
        [Column("REFERENCIA")]  
        public string Referencia { get; set; }
        [Column("DES_REFERENCIA")]  
        public string DesReferencia { get;set; }
        [Column("UBICACION")]   
        public string Ubicacion { get; set; }
    }
}
