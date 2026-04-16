using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GastroSAE
{
    /// <summary>
    /// Agrega “leyendas” de atajos de teclado (shortcuts) para accesibilidad.
    /// Para botones, se embebe el hint dentro del mismo botón en una segunda línea.
    /// Para otros controles, se crea un label justo debajo del control.
    /// </summary>
    public static class UiHints
    {
        private const int GapY = 2;

        public static void Attach(Form form, IDictionary<string, string> controlNameToHint)
        {
            if (controlNameToHint == null || controlNameToHint.Count == 0) return;

            var bindings = new List<(Control Control, Label Label)>();

            foreach (var kv in controlNameToHint)
            {
                var controlName = kv.Key;
                var hint = kv.Value;

                var c = FindByName(form, controlName);
                if (c == null) continue;

                // Para acciones, el botón es el target ideal (touch + teclado)
                if (c is Button btn)
                {
                    btn.Text = EmbedHintInButton(btn.Text, hint);
                    continue;
                }

                if (c is CheckBox chk)
                {
                    chk.Text = EmbedHintInline(chk.Text, hint);
                    continue;
                }

                // Si está dentro de un TableLayoutPanel, lo envolvemos para no romper el layout
                if (TryWrapInTableLayout(form, c, hint))
                    continue;

                var lbl = CreateHintLabel(form, hint);
                c.Parent?.Controls.Add(lbl);
                lbl.BringToFront();

                bindings.Add((c, lbl));
            }

            void RepositionAll(object? _, EventArgs __)
            {
                foreach (var (c, lbl) in bindings)
                {
                    if (c.Parent == null) continue;

                    // Si el control está invisible, también ocultamos hint
                    lbl.Visible = c.Visible;

                    // Coloca debajo y alinea al inicio
                    lbl.Location = new Point(c.Left, c.Bottom + GapY);
                    lbl.Width = Math.Max(60, c.Width);
                }
            }

            // Reposicionar cuando cambia layout (resize, anchors, etc.)
            form.Shown += (_, __) => RepositionAll(null, EventArgs.Empty);
            form.Layout += RepositionAll;
            form.Resize += RepositionAll;
        }

        
        public static void Attach(Form form, IEnumerable<(Control control, string hint)> hints)
        {
            if (hints == null) return;

            var bindings = new List<(Control Control, Label Label)>();

            foreach (var (c, hint) in hints)
            {
                if (c == null) continue;

                if (c is Button btn)
                {
                    btn.Text = EmbedHintInButton(btn.Text, hint);
                    continue;
                }

                if (c is CheckBox chk)
                {
                    chk.Text = EmbedHintInline(chk.Text, hint);
                    continue;
                }

                // Si está dentro de un TableLayoutPanel, lo envolvemos para no romper el layout
                if (TryWrapInTableLayout(form, c, hint))
                    continue;

                var lbl = CreateHintLabel(form, hint);
                c.Parent?.Controls.Add(lbl);
                lbl.BringToFront();
                bindings.Add((c, lbl));
            }

            void RepositionAll(object? _, EventArgs __)
            {
                foreach (var (c, lbl) in bindings)
                {
                    if (c.Parent == null) continue;
                    lbl.Visible = c.Visible;
                    lbl.Location = new Point(c.Left, c.Bottom + GapY);
                    lbl.Width = Math.Max(60, c.Width);
                }
            }

            form.Shown += (_, __) => RepositionAll(null, EventArgs.Empty);
            form.Layout += RepositionAll;
            form.Resize += RepositionAll;
        }


        private static bool TryWrapInTableLayout(Form form, Control c, string hint)
        {
            if (c.Parent is not TableLayoutPanel tlp) return false;

            var pos = tlp.GetPositionFromControl(c);
            int col = pos.Column;
            int row = pos.Row;
            if (col < 0 || row < 0) return false;

            int colSpan = tlp.GetColumnSpan(c);
            int rowSpan = tlp.GetRowSpan(c);
            var margin = c.Margin;

            var lbl = CreateHintLabel(form, hint);

            var wrap = new Panel
            {
                BackColor = Color.Transparent,
                Margin = margin,
                Dock = DockStyle.Fill
            };

            // Inputs tienden a ser auto-size en nuestras filas
            if (c is TextBox or ComboBox or NumericUpDown)
            {
                wrap.AutoSize = true;
                wrap.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }

            c.Margin = Padding.Empty;
            lbl.Margin = new Padding(0, GapY, 0, 0);

            if (c is DataGridView || c is ListBox || c is ListView)
            {
                c.Dock = DockStyle.Fill;
                lbl.Dock = DockStyle.Bottom;
                lbl.Height = 18;
                wrap.Controls.Add(lbl);
                wrap.Controls.Add(c);
            }
            else
            {
                c.Dock = DockStyle.Top;
                lbl.Dock = DockStyle.Top;
                lbl.Height = 18;
                wrap.Controls.Add(lbl);
                wrap.Controls.Add(c);
            }

            tlp.SuspendLayout();
            tlp.Controls.Remove(c);
            tlp.Controls.Add(wrap, col, row);
            tlp.SetColumnSpan(wrap, colSpan);
            tlp.SetRowSpan(wrap, rowSpan);
            tlp.ResumeLayout(true);

            return true;
        }

private static Label CreateHintLabel(Form form, string hint)
        {
            var size = Math.Max(8.5f, form.Font.Size - 2.5f);
            return new Label
            {
                AutoSize = false,
                Height = 16,
                Text = hint,
                Font = new Font(form.Font.FontFamily, size, FontStyle.Regular, GraphicsUnit.Point),
                // Hint discreto (tema claro)
                ForeColor = Color.FromArgb(98, 98, 98)
            };
        }

        private static string EmbedHintInButton(string original, string hint)
        {
            original ??= string.Empty;
            // Evitar duplicado
            if (original.Contains("\n")) return original;

            return $"{original}\n{hint}";
        }

        private static string EmbedHintInline(string original, string hint)
        {
            original ??= string.Empty;
            if (original.Contains("(") && original.Contains(")")) return original;
            return $"{original} ({hint})";
        }

        private static Control? FindByName(Control root, string name)
        {
            if (root.Name == name) return root;

            foreach (Control child in root.Controls)
            {
                var found = FindByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
