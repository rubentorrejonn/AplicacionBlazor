using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    [Table("PICKING_LOGS")]
    public class PickingLogs
    {
        [Column("ID")]
        public int Id { get; set; }

        [Column("PETICION")]
        public int Peticion { get; set; }

        [Column("PALET")]
        public int Palet { get; set; }
        [Column("PALET_RETIRADA")]
        public int? PaletRetirada { get; set; }

        [Column("REFERENCIA")]
        public string Referencia { get; set; } = null!;

        [Column("CANTIDAD_QUITADA")]
        public int CantidadQuitada { get; set; }

        [Column("FECHA_VERIFICACION")]
        public DateTime FechaVerificacion { get; set; }

        [Column("IDUSUARIO")]
        public int IdUsuario { get; set; }
        [Column("NOMBRE")]
        public string NombreUsuario { get; set; } = null!;
    }
}
