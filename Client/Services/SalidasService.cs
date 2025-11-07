using Microsoft.JSInterop;
using UltimateProyect.Shared.Models;

namespace UltimateProyect.Client.Services
{
    public class SalidasService
    {
        private readonly IJSRuntime _js;

        public SalidasService(IJSRuntime js)
        {
            _js = js;
            Carrito = new();
            StockDisponible = new();
        }

        public int? CurrentPeticion { get; private set; }
        public Dictionary<string, int> StockDisponible { get; set; }
        public Dictionary<string, OrdenSalidaLinDto> Carrito { get; set; }

        public void SetCurrentPeticion(int peticion)
        {
            CurrentPeticion = peticion;
            Carrito.Clear();
        }

        public void ActualizarStock(Dictionary<string, int> stock)
        {
            StockDisponible = stock;
        }

        public void AgregarAlCarrito(string referencia, string desReferencia)
        {
            if (CurrentPeticion == null)
                throw new InvalidOperationException("Debe seleccionar una petición primero.");

            if (Carrito.ContainsKey(referencia))
            {
                Carrito[referencia].Cantidad += 1;
            }
            else
            {
                Carrito[referencia] = new OrdenSalidaLinDto
                {
                    Peticion = CurrentPeticion.Value,
                    Referencia = referencia,
                    DesReferencia = desReferencia,
                    Cantidad = 1
                };
            }

            var stockMax = StockDisponible.GetValueOrDefault(referencia, 0);
            if (Carrito[referencia].Cantidad > stockMax)
                Carrito[referencia].Cantidad = stockMax;
        }

        public void Incrementar(string referencia)
        {
            if (CurrentPeticion == null)
                throw new InvalidOperationException("Debe seleccionar una petición primero.");

            if (Carrito.ContainsKey(referencia))
            {
                var stockMax = StockDisponible.GetValueOrDefault(referencia, 0);
                if (Carrito[referencia].Cantidad < stockMax)
                    Carrito[referencia].Cantidad++;
            }
        }

        public void Decrementar(string referencia)
        {
            if (CurrentPeticion == null)
                throw new InvalidOperationException("Debe seleccionar una petición primero.");

            if (Carrito.ContainsKey(referencia) && Carrito[referencia].Cantidad > 1)
            {
                Carrito[referencia].Cantidad--;
            }
        }

        public void Remove(string referencia)
        {
            if (Carrito.ContainsKey(referencia))
                Carrito.Remove(referencia);
        }

        public void Clear() => Carrito.Clear();

        public List<OrdenSalidaLinDto> ToLineasDto()
        {
            if (!CurrentPeticion.HasValue)
                throw new InvalidOperationException("No se ha establecido una petición para crear las líneas.");

            return Carrito.Values.Select((item, index) => new OrdenSalidaLinDto
            {
                Peticion = CurrentPeticion.Value,
                Linea = index + 1,
                Referencia = item.Referencia,
                Cantidad = item.Cantidad,
                DesReferencia = item.DesReferencia
            }).ToList();
        }
    }
}