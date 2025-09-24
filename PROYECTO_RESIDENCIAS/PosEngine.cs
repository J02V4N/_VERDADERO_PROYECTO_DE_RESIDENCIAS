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
                // NO asignes d.Importe.
                // Si cambió Cantidad, PesoGr o PrecioUnit, el getter de Importe recalculará solo.
                // Aquí podrías normalizar mínimos o redondeos, p. ej.:
                if (!d.RequierePeso)
                    d.Cantidad = Math.Max(0.01m, d.Cantidad);
                else
                    d.PesoGr = Math.Max(0m, d.PesoGr ?? 0m);
            }

            // NO asignes Subtotal/Impuesto/Total: están calculados en la clase Pedido.
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
                PrecioUnit = p.PrecioUnit // por ahora viene del seed/UI
            };
        }

        public static void Reindex(List<PedidoDet> det)
        {
            int i = 1; foreach (var x in det) x.Partida = i++;
        }
    }
}
