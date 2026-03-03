using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    /// <summary>
    /// Placeholders (cue banners) + descripciones accesibles para inputs.
    /// Objetivo: que cada campo “se explique solo” (touch + teclado).
    /// </summary>
    public static class UiFields
    {
        // Win32: cue banner para TextBox / ComboBox (funciona incluso en DropDownList)
        private const int EM_SETCUEBANNER = 0x1501;
        private const int CB_SETCUEBANNER = 0x1703;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        /// <summary>
        /// Aplica placeholders por nombre de control. Si no hay match, intenta inferir por prefijo.
        /// </summary>
        public static void Apply(Control root)
        {
            if (root == null) return;

            var map = BuildDefaultMap();

            foreach (var c in EnumerateControls(root))
            {
                // TextBoxes
                if (c is TextBox tb)
                {
                    var hint = ResolveHint(map, tb.Name);
                    if (!string.IsNullOrWhiteSpace(hint))
                    {
                        SetPlaceholder(tb, hint);
                        if (string.IsNullOrWhiteSpace(tb.AccessibleName)) tb.AccessibleName = hint;
                    }
                }

                // Combos
                if (c is ComboBox cb)
                {
                    var hint = ResolveHint(map, cb.Name);
                    if (!string.IsNullOrWhiteSpace(hint))
                    {
                        SetPlaceholder(cb, hint);
                        if (string.IsNullOrWhiteSpace(cb.AccessibleName)) cb.AccessibleName = hint;
                    }
                }

                // Otros controles “capturables”
                if ((c is ListBox || c is DataGridView) && string.IsNullOrWhiteSpace(c.AccessibleName))
                {
                    var hint = ResolveHint(map, c.Name);
                    if (!string.IsNullOrWhiteSpace(hint))
                        c.AccessibleName = hint;
                }
            }
        }

        private static IEnumerable<Control> EnumerateControls(Control root)
        {
            var stack = new Stack<Control>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var c = stack.Pop();
                foreach (Control child in c.Controls)
                {
                    yield return child;
                    if (child.HasChildren) stack.Push(child);
                }
            }
        }

        private static Dictionary<string, string> BuildDefaultMap()
        {
            // Mapeo “de negocio” (claro y en español)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cobro / CFDI
                ["txtRFC"] = "RFC (13 caracteres)",
                ["txtRazon"] = "Razón social",
                ["cboUsoCFDI"] = "Uso CFDI",
                ["cboMetodoPago"] = "Método de pago",
                ["cboFormaPago"] = "Forma de pago",
                ["txtImporteRecibido"] = "Importe recibido",

                ["txtCobroEfectivo"] = "Efectivo",
                ["txtCobroTarjeta"] = "Tarjeta",
                ["txtCobroRef"] = "Referencia tarjeta",

                // Pedido
                ["txtBuscarPlatillo"] = "Buscar platillo (Ctrl+K)…",
                ["lbPlatillos"] = "Lista de platillos",
                ["dgvPedido"] = "Detalle del pedido",
                ["dgvReceta"] = "Receta / ingredientes",

                // Inventario
                ["txtInvBuscar"] = "Buscar artículo (Ctrl+F)…",
                ["lbInvArticulos"] = "Artículos (SAE)",
                ["txtInvPesoGr"] = "Peso (gr)",
                ["txtInvCostoKg"] = "Costo por kg",
                ["dgvInvCaptura"] = "Captura de entradas",

                // Config
                ["txtRutaSae"] = "Ruta de datos SAE",
                ["txtRutaAux"] = "Ruta de datos AUX",
                ["cboImpresora"] = "Impresora de tickets",
                ["txtPuertoCom"] = "Puerto COM (ej. COM1)",
                ["cboAlmacen"] = "Almacén",
                ["cboListaPrecios"] = "Lista de precios",

                // Selección empresa
                ["txtUser"] = "Usuario Firebird (SYSDBA)",
                ["txtPass"] = "Password Firebird",
                ["txtServer"] = "Servidor (localhost o IP)",
                ["txtPort"] = "Puerto (3050)",
                ["cboBases"] = "Base de datos (.FDB)",

                // Turno
                ["txtResponsable"] = "Responsable del turno",
                ["txtObs"] = "Observación (opcional)",
            };
        }

        private static string? ResolveHint(Dictionary<string, string> map, string? controlName)
        {
            if (string.IsNullOrWhiteSpace(controlName)) return null;
            if (map.TryGetValue(controlName, out var hit)) return hit;

            // Inferencia básica por prefijos comunes
            var n = controlName;
            if (n.StartsWith("txt", StringComparison.OrdinalIgnoreCase)) n = n[3..];
            if (n.StartsWith("cbo", StringComparison.OrdinalIgnoreCase)) n = n[3..];
            if (n.StartsWith("lb", StringComparison.OrdinalIgnoreCase)) n = n[2..];
            if (n.StartsWith("dgv", StringComparison.OrdinalIgnoreCase)) n = n[3..];

            // Divide camelCase/PascalCase
            var spaced = string.Concat(n.Select((ch, i) => i > 0 && char.IsUpper(ch) ? " " + ch : ch.ToString()));

            // Traducciones rápidas de tokens
            spaced = spaced
                .Replace("Inv ", "Inventario ")
                .Replace("Cobro ", "Cobro ")
                .Replace("Rfc", "RFC")
                .Replace("Razon", "Razón social")
                .Replace("Buscar", "Buscar")
                .Trim();

            return string.IsNullOrWhiteSpace(spaced) ? null : spaced;
        }

        private static void SetPlaceholder(TextBox tb, string text)
        {
            // Si el framework soporta PlaceholderText, úsalo.
            try
            {
                var prop = tb.GetType().GetProperty("PlaceholderText");
                if (prop != null)
                {
                    var current = prop.GetValue(tb) as string;
                    if (string.IsNullOrWhiteSpace(current))
                        prop.SetValue(tb, text);
                    return;
                }
            }
            catch { /* fallback Win32 */ }

            try
            {
                if (tb.IsHandleCreated)
                    SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
                else
                    tb.HandleCreated += (_, __) => SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, text);
            }
            catch { /* no-op */ }
        }

        private static void SetPlaceholder(ComboBox cb, string text)
        {
            // ComboBox no tiene PlaceholderText oficial: usamos cue banner nativo.
            try
            {
                if (cb.IsHandleCreated)
                    SendMessage(cb.Handle, CB_SETCUEBANNER, (IntPtr)0, text);
                else
                    cb.HandleCreated += (_, __) => SendMessage(cb.Handle, CB_SETCUEBANNER, (IntPtr)0, text);
            }
            catch { /* no-op */ }
        }
    }
}
