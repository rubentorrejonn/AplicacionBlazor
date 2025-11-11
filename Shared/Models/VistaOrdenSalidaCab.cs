using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class VistaOrdenSalidaCab
    {
        
        [Column("PETICION")]
        public int Peticion { get; set; }

        [Column("NOMBRE_CLIENTE")]
        public string NombreCliente { get; set; } = null!;

        [Column("DIRECCION_ENTREGA")]
        public string DireccionEntrega { get; set; } = null!;

        [Column("CODIGO_POSTAL")]
        public int CodigoPostal { get; set; }

        [Column("POBLACION")]
        public string Poblacion { get; set; } = null!;

        [Column("PROVINCIA")]
        public string Provincia { get; set; } = null!;

        [Column("TELEFONO")]
        public string Telefono { get; set; } = null!;

        [Column("F_CREACION")]
        public DateTime FCreacion { get; set; }

        [Column("ESTADO")]
        public int Estado { get; set; }

        [Column("DESCRIPCION")]
        public string DesEstado { get; set; } = null!;
    }
}
