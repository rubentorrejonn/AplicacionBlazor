using Microsoft.AspNetCore.Identity;

namespace UltimateProyect.Server.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string Nombre { get; set; } = null!;
        public string CodigoPostal { get; set; } = null!;
        public bool Activo { get; set; }
    }
}