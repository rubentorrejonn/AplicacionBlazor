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
        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        public string NombreCliente { get; set; } = null!;
        [Column("DIRECCION_ENTREGA")]
        [Required(ErrorMessage = "La dirección de entrega es obligatoria.")]
        public string DireccionEntrega { get; set; } = null!;
        [Column("CODIGO_POSTAL")]
        [Required(ErrorMessage = "El código postal es obligatorio.")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "El código postal debe tener 5 dígitos.")]
        public int CodigoPostal { get; set; }
        [Column("POBLACION")]
        [Required(ErrorMessage = "La población es obligatoria.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$", ErrorMessage = "La población solo puede contener letras (sin espacios).")]
        public string Poblacion { get; set; } = null!;
        [Column("PROVINCIA")]
        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ]+$", ErrorMessage = "La provincia solo puede contener letras (sin espacios).")]
        public string Provincia { get; set; } = null!;
        [Column("TELEFONO")]
        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El teléfono debe tener 9 dígitos.")]
        public string Telefono { get; set; } = null!;
        [Column("F_CREACION")]
        public DateTime FCreacion { get; set; } = DateTime.Now;
        [Column("ESTADO")]
        public int Estado { get; set; }
    }
}