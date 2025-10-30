using Microsoft.JSInterop;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Client.Services
{
    public class RecepcionService
    {
        private readonly IJSRuntime _js;

        public RecepcionService(IJSRuntime js)
        {
            _js = js;
            Carrito = new();
        }

        public int? CurrentAlbaran { get; private set; }
        public Dictionary<string, RecepcionLineaDto> Carrito { get; set; }

        public void SetCurrentAlbaran(int albaran)
        {
            CurrentAlbaran = albaran;
            Carrito.Clear();
        }

        public void AddOrIncrement(
            string referencia,
            string desReferencia,
            bool requiereNSerie,
            int? longNSerie,
            int cantidad = 1,
            int bien = 0,
            int mal = 0)
        {
            if (CurrentAlbaran == null)
                throw new InvalidOperationException("Debe seleccionar un albarán primero.");

            if (Carrito.ContainsKey(referencia))
            {
                
                Carrito[referencia].Cantidad += cantidad;
                Carrito[referencia].Bien = bien;
                Carrito[referencia].Mal = mal;
            }
            else
            {
                Carrito[referencia] = new RecepcionLineaDto
                {
                    Referencia = referencia,
                    DesReferencia = desReferencia,
                    Cantidad = cantidad,
                    Bien = bien,
                    Mal = mal,
                    RequiereNSerie = requiereNSerie,
                    LongNSerie = longNSerie
                };
            }
        }

        public void Remove(string referencia)
        {
            if (Carrito.ContainsKey(referencia))
                Carrito.Remove(referencia);
        }

        public void Clear() => Carrito.Clear();

        public List<RecepcionLineaDto> ToLineasDto()
        {
            return Carrito.Values.Select((item, index) => new RecepcionLineaDto
            {
                Albaran = CurrentAlbaran.Value,
                Linea = index + 1,
                Referencia = item.Referencia,
                Cantidad = item.Cantidad,
                // Bien y Mal se ignoran en la API → no los enviamos (o los dejamos en 0)
                Bien = 0,
                Mal = 0,
                DesReferencia = item.DesReferencia,
                RequiereNSerie = false, // No relevante aquí
                LongNSerie = null,
                NumerosSerieBien = new(),
                NumerosSerieMal = new()
            }).ToList();
        }
    }
}
