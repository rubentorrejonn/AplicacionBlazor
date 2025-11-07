using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class OrdenSalidaLinDto
    {
        public string Referencia { get; set; } = null!;
        public int Peticion { get; set; }
        public string DesReferencia { get; set; } = null!;
        public int Linea { get; set; }
        public int Cantidad { get; set; }
    }

}
