using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    /// <summary>
    /// Estilo y ajustes de accesibilidad (touch + teclado) para WinForms.
    /// Se aplica en runtime para no pelearse con el Designer.
    /// </summary>
    public static class UiStyle
    {
        // Ajusta aquí si quieres todo más grande/pequeño
        public const float BaseFontSize = 11.0f;

        // Tema “restaurante”: claro/pastel + acento vino.
        // Objetivo: tranquilo, legible y con contraste en botones.
        private static readonly Color ThemeBg = Color.FromArgb(246, 241, 238);       // marfil rosado (pastel)
        private static readonly Color ThemeSurface = Color.FromArgb(255, 255, 255); // blanco
        private static readonly Color ThemeAccent = Color.FromArgb(194, 31, 67);     // vino
        private static readonly Color ThemeText = Color.FromArgb(28, 28, 28);        // casi negro
        private static readonly Color ThemeMuted = Color.FromArgb(98, 98, 98);
        private static readonly Color ThemeInputBg = Color.FromArgb(255, 255, 255);
        private static readonly Color ThemeInputText = Color.FromArgb(28, 28, 28);

        // Targets táctiles típicos (~44px alto).
        // IMPORTANTE: NO forzamos Height en runtime (puede encimar controles en layouts fijos).
        // En vez de eso usamos padding + tipografía y anclajes.
        private const int TouchButtonMinH = 44;
        private const int TouchInputMinH = 32;

        public static void Apply(Form form)
        {
            // DPI correcto (pantallas táctiles / tablets suelen venir con DPI alto)
            try { form.AutoScaleMode = AutoScaleMode.Dpi; } catch { /* no-op */ }

            // Fuente moderna (Win10/11). OJO: no forzamos colores agresivos.
            form.Font = new Font("Segoe UI", BaseFontSize, FontStyle.Regular, GraphicsUnit.Point);

            // Tema
            try
            {
                form.BackColor = ThemeBg;
                form.ForeColor = ThemeText;
            }
            catch { /* no-op */ }

            // Permite que el Form capture shortcuts sin depender del control enfocado
            form.KeyPreview = true;

            // Padding ligero para que no se sienta “apretado”
            if (form.Padding.Left < 8) form.Padding = new Padding(8);

            ApplyToControls(form);

            // Ajustes de anclaje para que los componentes internos acompañen el resize.
            // (WinForms no es responsive real, pero esto elimina el 80% del dolor.)
            UiLayout.Apply(form);
        }

        public static void ApplyToControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                ApplyControl(c);

                // Recurse
                if (c.HasChildren)
                    ApplyToControls(c);
            }
        }

        private static void ApplyControl(Control c)
        {
            // Fuente consistente
            c.Font = new Font("Segoe UI", BaseFontSize, FontStyle.Regular, GraphicsUnit.Point);

            // Tabs grandes y fáciles de tocar
            if (c is TabControl tc)
            {
                tc.SizeMode = TabSizeMode.Fixed;
                if (tc.ItemSize.Height < 42)
                    tc.ItemSize = new Size(Math.Max(tc.ItemSize.Width, 140), 42);

                // Fondo (TabControl no pinta perfecto en oscuro sin OwnerDraw,
                // pero al menos armonizamos páginas).
                try
                {
                    tc.BackColor = ThemeBg;
                    tc.ForeColor = ThemeText;
                }
                catch { /* no-op */ }

                return;
            }

            if (c is TabPage tp)
            {
                try
                {
                    tp.BackColor = ThemeBg;
                    tp.ForeColor = ThemeText;
                    tp.AutoScroll = true; // “acoplable” en pantallas pequeñas
                    tp.Padding = new Padding(Math.Max(tp.Padding.Left, 10));
                }
                catch { /* no-op */ }
                return;
            }

            if (c is Panel p)
            {
                try
                {
                    if (p.BackColor == SystemColors.Control)
                        p.BackColor = ThemeSurface;
                    p.ForeColor = ThemeText;
                }
                catch { /* no-op */ }
                // no return: puede contener otros controles
            }

            if (c is GroupBox gb)
            {
                try
                {
                    gb.ForeColor = ThemeText;
                    gb.BackColor = ThemeSurface;
                }
                catch { /* no-op */ }
            }

            if (c is Label lbl)
            {
                // Labels con el tema
                try
                {
                    if (lbl.ForeColor == SystemColors.ControlText)
                        lbl.ForeColor = ThemeText;
                }
                catch { /* no-op */ }
            }

            // Grids legibles (toque y selección)
            if (c is DataGridView dgv)
            {
                dgv.RowTemplate.Height = Math.Max(dgv.RowTemplate.Height, 34);
                dgv.ColumnHeadersHeight = Math.Max(dgv.ColumnHeadersHeight, 40);
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.MultiSelect = false;

                // Look & feel
                try
                {
                    dgv.BackgroundColor = ThemeInputBg;
                    dgv.GridColor = Color.Gainsboro;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = ThemeAccent;
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    dgv.DefaultCellStyle.BackColor = ThemeInputBg;
                    dgv.DefaultCellStyle.ForeColor = ThemeInputText;
                    dgv.DefaultCellStyle.SelectionBackColor = ThemeAccent;
                    dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                }
                catch { /* no-op */ }
                return;
            }

            // StatusStrip legible
            if (c is StatusStrip ss)
            {
                ss.Font = new Font("Segoe UI", BaseFontSize - 1.0f, FontStyle.Regular, GraphicsUnit.Point);
                try
                {
                    ss.BackColor = ThemeSurface;
                    ss.ForeColor = ThemeText;
                }
                catch { /* no-op */ }
                return;
            }

            // Botones: tamaño táctil + estilo sobrio (primario vs secundario)
            if (c is Button b)
            {
                b.AutoSize = false;
                b.MinimumSize = new Size(Math.Max(b.MinimumSize.Width, 120), Math.Max(b.MinimumSize.Height, TouchButtonMinH));

                try
                {
                    b.FlatStyle = FlatStyle.Flat;
                    b.Padding = new Padding(10, 6, 10, 6);

                    // Clasificación simple (no perfecta, pero suficiente y centralizada)
                    string key = (b.Name + " " + b.Text).ToLowerInvariant();
                    bool isPrimary = key.Contains("confirm") || key.Contains("guardar") || key.Contains("cobrar") || key.Contains("ir a cobro") || key.Contains("abrir") || key.Contains("atender") || key.Contains("continuar");
                    bool isDanger = key.Contains("eliminar") || key.Contains("borrar") || key.Contains("liberar") || key.Contains("cancel");

                    if (isPrimary)
                    {
                        b.BackColor = ThemeAccent;
                        b.ForeColor = Color.White;
                        b.FlatAppearance.BorderSize = 0;
                    }
                    else if (isDanger)
                    {
                        // Peligro: mismo acento, pero un poco más sobrio
                        b.BackColor = Color.FromArgb(166, 25, 53);
                        b.ForeColor = Color.White;
                        b.FlatAppearance.BorderSize = 0;
                    }
                    else
                    {
                        // Secundario: blanco/pastel con borde vino
                        b.BackColor = ThemeSurface;
                        b.ForeColor = ThemeAccent;
                        b.FlatAppearance.BorderSize = 1;
                        b.FlatAppearance.BorderColor = ThemeAccent;
                        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 228, 233);
                    }
                }
                catch { /* no-op */ }
                return;
            }


            // Entradas: un poco más altas SOLO si hay espacio
            if (c is TextBox tb)
            {
                try
                {
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.AutoSize = false;
                    // No forzamos Height: solo mejoramos legibilidad.
                    tb.MinimumSize = new Size(tb.MinimumSize.Width, Math.Max(tb.MinimumSize.Height, TouchInputMinH));

                    tb.BackColor = ThemeInputBg;
                    tb.ForeColor = ThemeInputText;
                }
                catch { /* no-op */ }
                return;
            }

            if (c is NumericUpDown nud)
            {
                nud.MinimumSize = new Size(nud.MinimumSize.Width, Math.Max(nud.MinimumSize.Height, TouchInputMinH));
                return;
            }

            if (c is ListBox lb)
            {
                lb.IntegralHeight = false;
                try
                {
                    lb.BackColor = ThemeInputBg;
                    lb.ForeColor = ThemeInputText;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                }
                catch { /* no-op */ }
                return;
            }

            if (c is ComboBox cb)
            {
                cb.IntegralHeight = false;
                // Evitamos forzar Height para no romper layouts tight
                try
                {
                    cb.BackColor = ThemeInputBg;
                    cb.ForeColor = ThemeInputText;
                }
                catch { /* no-op */ }
                return;
            }
        }

        // Nota: intencionalmente SIN lógica de "inflar" alturas.
        // El inflado en layouts fijos es la razón #1 de encimados.
    }
}
