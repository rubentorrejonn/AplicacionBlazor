using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class OrdenSalidaCab
    {
        [Column("PETICION")]
        public int Peticion {  get; set; }
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
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El telefono tiene que tener 9 digitos")]
        public string Telefono { get; set; } = null!;
        [Column("F_CREACION")]
        public DateTime FCreacion { get; set; } = DateTime.Now;
        [Column("ESTADO")]
        public int Estado { get; set; }
    }
}