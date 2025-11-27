using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class PaletVerificacionDto
    {
        public int Palet { get; set; }
        public string Ubicacion { get; set; } = null!;
        public int Cantidad { get; set; }
        public List<string> NumerosSerieValidos { get; set; } = new();
    }
}
