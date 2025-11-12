using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class Usuarios
    {
        [Column("IDUSUARIO")]
        public int IdUsuario { get; set; }
        [Column("NOMBRE")]
        public string Nombre { get; set; } = null!;
        [Column("USERNAME")]
        public string? UserName { get; set; }
        [Column("Password")]
        public string? Password { get; set; }
        [Column("EMAIL")]
        public string Email { get; set; } = null!;
        [Column("TELEFONO")]
        public int Telefono { get; set; }
        [Column("CODIGO_POSTAL")]
        public string CodigoPostal { get; set; } = null!;
        [Column("ACTIVO")]
        public bool? Activo { get; set; }
        [Column("ROLE")]
        public string? Role { get; set; }
    }
}
