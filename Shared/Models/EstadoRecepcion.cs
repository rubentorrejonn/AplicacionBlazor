using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class EstadoRecepcion
    {
        [Column("ID_ESTADO")]
        public int IdEstado { get; set; }
        [Column("CAMPO")]
        public string Campo { get; set; }
        [Column("TABLA")]
        public string Tabla { get; set; }
        [Column("ESTADO")]
        public int Estado { get; set; }
        [Column("DESCRIPCION")]
        public string Descripcion { get; set; }
    }
}
