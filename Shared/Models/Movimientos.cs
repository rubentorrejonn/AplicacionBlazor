using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class Movimientos
    {
        [Column("ID_MOVIMIENTOS")]
        public int IdMovimientos { get; set; }
        [Column("PETICION")]
        public int Peticion { get; set; }
        [Column("PALET")]
        public int Palet { get; set; }
        [Column("CANTIDAD")]
        public int Cantidad { get; set; }
        [Column("REFERENCIA")]
        public string Referencia { get; set; }
        [Column("UBICACION_ORIGEN")]
        public string UbicacionOrigen { get; set; }
        [Column("UBICACION_DESTINO")]
        public string UbicacionDestino { get; set; }
        [Column("LIN_PETICION")]
        public int LinPeticion { get; set; }
        [Column("REALIZADO")]
        public int Realizado { get; set; }
    }
}
