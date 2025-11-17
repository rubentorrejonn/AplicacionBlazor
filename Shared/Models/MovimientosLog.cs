using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace UltimateProyect.Shared.Models
{
    public class MovimientosLog
    {
        [Column("ID_MOVIMIENTOS")]
        public int IdMovimientos { get; set; }
        [Column("PETICION")]
        public int Peticion { get; set; }
        [Column("PALET")]
        public int Palet { get; set; }
        [Column("REFERENCIA")]
        public int Referencia { get; set; }
        [Column("FECHA_MOVIMIENTO")]
        public DateTime fechaMovimiento { get; set; }
        [Column("UBICACION_ORIGEN")]
        public string UbicacionOrigen { get; set; }
        [Column("UBICACION_DESTINO")]
        public string UbicacionDestino { get; set; }
        //FK
        [Column("IDUSUARIO")]
        public int IdUsuario
        {
            get; set;

        }
    }
}