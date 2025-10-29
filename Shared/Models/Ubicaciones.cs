using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class Ubicaciones
    {
        //PK
        [Column("UBICACION")]
        public string Ubicacion { get; set; } = null!;
        [Column("DES_UBICACION")]
        public string DesUbicacion { get; set; } = null!;
        [Column("CALLE")]
        public string Calle { get; set; } = null!;
        [Column("POSICION")]
        public int Posicion { get; set; }
    }
}
