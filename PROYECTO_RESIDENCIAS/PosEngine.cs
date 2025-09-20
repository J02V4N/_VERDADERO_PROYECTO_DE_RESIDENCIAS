using System;
using System.Collections.Generic;
using System.Linq;
using static PROYECTO_RESIDENCIAS.Form1;

namespace PROYECTO_RESIDENCIAS
{
    public static class PosEngine
    {
        public static void RecalcularPedido(Pedido pedido, decimal ivaPct = 0.16m)
        {
            if (pedido == null) return;

            foreach (var d in pedido.Detalles)
            {
                decimal baseCalc = d.RequierePeso
                    ? Math.Max(0m, (d.PesoGr ?? 0m) / 1000m)       // gramos -> kg
                    : Math.Max(0m, d.Cantidad);

                d.Importe = Math.Round(baseCalc * d.PrecioUnit, 2, MidpointRounding.AwayFromZero);
            }

            pedido.Subtotal = Math.Round(pedido.Detalles.Sum(x => x.Importe), 2, MidpointRounding.AwayFromZero);
            pedido.Impuesto = Math.Round(pedido.Subtotal * ivaPct, 2, MidpointRounding.AwayFromZero);
            pedido.Total = Math.Round(pedido.Subtotal + pedido.Impuesto, 2, MidpointRounding.AwayFromZero);
        }

        public static PedidoDet CrearPartida(Platillo p, decimal? pesoGr = null, decimal? cantidad = 1m)
        {
            return new PedidoDet
            {
                Clave = p.Clave,
                Nombre = p.Nombre,
                RequierePeso = p.RequierePeso,
                PesoGr = p.RequierePeso ? (pesoGr ?? 0m) : null,
                Cantidad = p.RequierePeso ? 1m : Math.Max(0.01m, cantidad ?? 1m),
                PrecioUnit = p.Precio, // por ahora viene del seed/UI
            };
        }

        public static void Reindex(List<PedidoDet> det)
        {
            int i = 1; foreach (var x in det) x.Partida = i++;
        }
    }
}
