using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class VistaPaletsReservados
    {
        public int Peticion { get; set; }
        public int Palet { get; set; }
        public int Cantidad { get; set; }
        public string Referencia { get; set; }
        public string DesReferencia { get;set; }
        public string Ubicacion { get; set; }
    }
}
