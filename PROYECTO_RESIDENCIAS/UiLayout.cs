using System;
using System.Linq;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    /// <summary>
    /// Heurísticas de layout para WinForms:
    /// - Evita que al redimensionar el Form el contenido se quede "congelado".
    /// - NO intenta hacer reflow tipo web (WinForms no está hecho para eso),
    ///   pero ancla/dockea lo importante para que sea usable en cualquier resolución.
    ///
    /// Se aplica en runtime para no reventar el Designer.
    /// </summary>
    public static class UiLayout
    {
        public static void Apply(Control root)
        {
            if (root == null) return;
            ApplyContainer(root);
        }

        private static void ApplyContainer(Control container)
        {
            // Recurse primero
            foreach (Control c in container.Controls)
            {
                if (c.HasChildren)
                    ApplyContainer(c);
            }

            // Si es un contenedor principal, mejora resize
            if (container is Form f)
            {
                f.MinimumSize = new System.Drawing.Size(Math.Max(f.MinimumSize.Width, 1050), Math.Max(f.MinimumSize.Height, 720));
            }

            // Heurística por control
            foreach (Control c in container.Controls)
            {
                try
                {
                    ApplyControlHeuristics(container, c);
                }
                catch
                {
                    // no-op: nunca reventar la app por layout
                }
            }
        }

        private static void ApplyControlHeuristics(Control parent, Control c)
        {
            // Si ya tiene Dock explícito distinto de None, respétalo.
            if (c.Dock != DockStyle.None)
                return;

            int pw = Math.Max(1, parent.ClientSize.Width);
            int ph = Math.Max(1, parent.ClientSize.Height);

            // Grids y listas: suelen ser la pieza grande que debe crecer
            if (c is DataGridView || c is ListBox || c is ListView)
            {
                bool bigW = c.Width >= pw * 0.45;
                bool bigH = c.Height >= ph * 0.25;

                if (bigW && bigH)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    return;
                }

                // Si está claramente a la izquierda y alto, que crezca vertical
                if (c.Left <= 12 && bigH)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
                    return;
                }
            }

            // TextBox/ComboBox: ancho flexible
            if (c is TextBox || c is ComboBox || c is NumericUpDown)
            {
                bool wide = c.Width >= pw * 0.35;
                if (wide)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    return;
                }
            }

            // GroupBox/Panel: si ocupan gran área, que se estiren
            if (c is GroupBox || c is Panel)
            {
                bool bigW = c.Width >= pw * 0.60;
                bool bigH = c.Height >= ph * 0.35;
                if (bigW && bigH)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                    return;
                }
            }

            // Botones: típicamente se quedan abajo-derecha o arriba-derecha
            if (c is Button)
            {
                bool right = c.Right >= pw - 12;
                bool bottom = c.Bottom >= ph - 12;

                if (right && bottom)
                {
                    c.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                    return;
                }
                if (right && c.Top <= 20)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    return;
                }
            }

            // Labels: si son "header" centrados, ancho flexible
            if (c is Label)
            {
                bool wide = c.Width >= pw * 0.50;
                if (wide)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    return;
                }
            }

            // Default: respeta lo existente (muchos controles chicos no deben moverse)
        }
    }
}
