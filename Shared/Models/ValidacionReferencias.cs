using System.ComponentModel.DataAnnotations;

namespace UltimateProyect.Shared.Models
{
    public class ValidacionReferencia : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string referencia)
            {
                if (string.IsNullOrWhiteSpace(referencia))
                    return new ValidationResult("La referencia no puede estar vacía.");

                if (referencia != referencia.Trim())
                    return new ValidationResult("La referencia no puede tener espacios al principio ni al final.");

                if (referencia.Trim().Length == 0)
                    return new ValidationResult("La referencia no puede ser solo espacios.");

                
                if (!System.Text.RegularExpressions.Regex.IsMatch(referencia, @"^[a-zA-Z0-9\-_]+$"))
                    return new ValidationResult("La referencia solo puede contener letras, números, guiones y guiones bajos.");
                

                return ValidationResult.Success;
            }

            return new ValidationResult("El valor de la referencia no es válido.");
        }
    }
}