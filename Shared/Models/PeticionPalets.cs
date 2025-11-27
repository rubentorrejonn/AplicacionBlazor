using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class PeticionPalets
    {
        public int Id { get; set; }
        public int Peticion { get; set; }
        public int Palet { get; set; }
        public int Cantidad { get; set; }
        public string Referencia { get; set; }
        public DateTime FechaAsignacion { get; set; } = DateTime.Now;
    }
}
