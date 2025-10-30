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

        public Dictionary<string, int> StockDisponible { get; set; }
        public Dictionary<string, OrdenSalidaLinDto> Carrito { get; set; }

        public void ActualizarStock(Dictionary<string, int> stock)
        {
            StockDisponible = stock;
        }

        public void AgregarAlCarrito(string referencia, string desReferencia)
        {
            if (Carrito.ContainsKey(referencia))
            {
                Carrito[referencia].Cantidad += 1;
            }
            else
            {
                Carrito[referencia] = new OrdenSalidaLinDto
                {
                    Referencia = referencia,
                    DesReferencia = desReferencia,
                    Cantidad = 1
                };
            }

            // Asegurar que no supere el stock
            var stockMax = StockDisponible.GetValueOrDefault(referencia, 0);
            if (Carrito[referencia].Cantidad > stockMax)
                Carrito[referencia].Cantidad = stockMax;
        }

        public void Incrementar(string referencia)
        {
            var stockMax = StockDisponible.GetValueOrDefault(referencia, 0);
            if (Carrito[referencia].Cantidad < stockMax)
                Carrito[referencia].Cantidad++;
        }

        public void Decrementar(string referencia)
        {
            if (Carrito[referencia].Cantidad > 1)
                Carrito[referencia].Cantidad--;
        }

        public void Eliminar(string referencia)
        {
            Carrito.Remove(referencia);
        }

        public void Clear() => Carrito.Clear();

        public List<OrdenSalidaLinDto> ToLineasDto() => Carrito.Values.ToList();
    }
}