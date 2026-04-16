using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GastroSAE
{
    public class FormAlertas : Form
    {
        private static readonly Color ThemeBg = Color.FromArgb(246, 241, 238);
        private static readonly Color ThemeSurface = Color.White;
        private static readonly Color ThemeAccent = Color.FromArgb(194, 31, 67);
        private static readonly Color ThemeText = Color.FromArgb(28, 28, 28);
        private static readonly Color ThemeMuted = Color.FromArgb(98, 98, 98);
        private static readonly Color ThemeWarn = Color.FromArgb(214, 120, 26);

        private readonly Panel _contentHost = new() { Dock = DockStyle.Fill, BackColor = ThemeSurface, Padding = new Padding(12) };
        private readonly Dictionary<string, Panel> _views = new();
        private readonly Dictionary<string, Panel> _cards = new();
        private string _activeKey = string.Empty;

        public FormAlertas(
            IReadOnlyList<SaeDb.InsumoAlertDto> agotados,
            IReadOnlyList<SaeDb.InsumoAlertDto> bajos,
            IReadOnlyList<SaeDb.PlatilloDisponibilidadDto> noDisponibles,
            IReadOnlyList<SaeDb.PlatilloDisponibilidadDto> limitados)
        {
            Text = "Centro de alertas";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1080, 680);
            Size = new Size(1280, 800);
            Font = new Font("Segoe UI", 10F);
            BackColor = ThemeBg;
            ForeColor = ThemeText;
            Padding = new Padding(10);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = ThemeBg,
                Padding = new Padding(10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var hdr = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = ThemeBg,
                Padding = new Padding(0, 0, 0, 8)
            };
            var lblTitulo = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                Text = "Centro de alertas",
                ForeColor = ThemeText
            };
            var lblSub = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 10F),
                Text = "Consulta rápida de insumos críticos y disponibilidad real de platillos.",
                ForeColor = ThemeMuted
            };
            hdr.Controls.Add(lblSub);
            hdr.Controls.Add(lblTitulo);
            root.Controls.Add(hdr, 0, 0);

            var cardsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                AutoSize = true,
                BackColor = ThemeBg,
                Margin = new Padding(0, 0, 0, 8)
            };
            for (int i = 0; i < 4; i++) cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            root.Controls.Add(cardsPanel, 0, 1);

            _views["agotados"] = MakeInsumosView("Insumos agotados", agotados, "Insumos sin existencia disponible.");
            _views["bajos"] = MakeInsumosView("Bajo mínimo", bajos, "Insumos que ya están por debajo del stock mínimo.");
            _views["nodisp"] = MakePlatillosView("Platillos no disponibles", noDisponibles, "Platillos que no pueden prepararse con el inventario actual.");
            _views["limitados"] = MakePlatillosView("Platillos limitados", limitados, "Platillos que todavía pueden venderse, pero con pocas porciones disponibles.");

            var cardAgot = MakeCard("agotados", "Insumos agotados", agotados.Count, Color.Firebrick, "Ver detalle");
            var cardBajos = MakeCard("bajos", "Bajo mínimo", bajos.Count, ThemeWarn, "Ver detalle");
            var cardNoDisp = MakeCard("nodisp", "Platillos no disponibles", noDisponibles.Count, Color.Firebrick, "Ver detalle");
            var cardLimit = MakeCard("limitados", "Platillos limitados", limitados.Count, ThemeWarn, "Ver detalle");

            cardsPanel.Controls.Add(cardAgot, 0, 0);
            cardsPanel.Controls.Add(cardBajos, 1, 0);
            cardsPanel.Controls.Add(cardNoDisp, 2, 0);
            cardsPanel.Controls.Add(cardLimit, 3, 0);

            var contentWrap = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = ThemeBg,
                Padding = new Padding(0, 4, 0, 6)
            };
            var lblAyuda = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Haz clic en cualquiera de los cuadros para cambiar la vista.",
                ForeColor = ThemeMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            contentWrap.Controls.Add(lblAyuda);
            root.Controls.Add(contentWrap, 0, 2);

            var contentBorder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeSurface,
                Padding = new Padding(1)
            };
            contentBorder.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(222, 222, 222));
                var rect = contentBorder.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(pen, rect);
            };
            contentBorder.Controls.Add(_contentHost);
            root.Controls.Add(contentBorder, 0, 3);

            ActivateView("agotados");
        }

        private Panel MakeCard(string key, string titulo, int cantidad, Color accent, string cta)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 118,
                Margin = new Padding(6),
                Padding = new Padding(14, 12, 14, 12),
                BackColor = ThemeSurface,
                Cursor = Cursors.Hand,
                Tag = key
            };

            pnl.Paint += (_, e) =>
            {
                var rect = pnl.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var border = new Pen(_activeKey == key ? ThemeAccent : Color.FromArgb(226, 226, 226), _activeKey == key ? 2f : 1f);
                e.Graphics.DrawRectangle(border, rect);
                using var accentBrush = new SolidBrush(accent);
                e.Graphics.FillRectangle(accentBrush, new Rectangle(0, 0, 8, pnl.Height));
            };

            void Activate(object? s, EventArgs e) => ActivateView(key);
            pnl.Click += Activate;

            var lblTitulo = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = titulo,
                ForeColor = ThemeMuted,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            lblTitulo.Click += Activate;

            var lblCant = new Label
            {
                Dock = DockStyle.Top,
                Height = 46,
                Text = cantidad.ToString("N0"),
                ForeColor = accent,
                Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            lblCant.Click += Activate;

            var lblCta = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Text = cta,
                ForeColor = ThemeAccent,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.BottomLeft
            };
            lblCta.Click += Activate;

            pnl.Controls.Add(lblCta);
            pnl.Controls.Add(lblCant);
            pnl.Controls.Add(lblTitulo);
            _cards[key] = pnl;
            return pnl;
        }

        private Panel MakeInsumosView(string titulo, IReadOnlyList<SaeDb.InsumoAlertDto> items, string subtitulo)
        {
            var wrap = CreateViewShell(titulo, subtitulo, items.Count);
            var grid = BaseGrid();
            grid.Columns.Add(TextCol("Clave", "Clave", 130));
            grid.Columns.Add(TextCol("Descripcion", "Descripción", 280, true));
            grid.Columns.Add(TextCol("Unidad", "UM", 70));
            grid.Columns.Add(TextCol("ExistenciaDisplay", "Existencia", 110));
            grid.Columns.Add(TextCol("StockMinDisplay", "Stock mín.", 110));
            grid.Columns.Add(TextCol("Motivo", "Motivo", 240, true));
            grid.DataSource = items.ToList();
            wrap.Controls.Add(grid);
            grid.BringToFront();
            return wrap;
        }

        private Panel MakePlatillosView(string titulo, IReadOnlyList<SaeDb.PlatilloDisponibilidadDto> items, string subtitulo)
        {
            var wrap = CreateViewShell(titulo, subtitulo, items.Count);
            var grid = BaseGrid();
            grid.Columns.Add(TextCol("Clave", "Clave", 140));
            grid.Columns.Add(TextCol("Descripcion", "Platillo", 300, true));
            grid.Columns.Add(TextCol("Precio", "Precio", 90));
            grid.Columns.Add(TextCol("PorcionesDisplay", "Porciones", 90));
            grid.Columns.Add(TextCol("Estado", "Estado", 120));
            grid.Columns.Add(TextCol("Motivo", "Motivo", 280, true));
            grid.DataSource = items.Select(x => new
            {
                x.Clave,
                x.Descripcion,
                Precio = x.Precio.ToString("N2"),
                x.PorcionesDisplay,
                x.Estado,
                x.Motivo
            }).ToList();
            wrap.Controls.Add(grid);
            grid.BringToFront();
            return wrap;
        }

        private static Panel CreateViewShell(string titulo, string subtitulo, int total)
        {
            var shell = new Panel { Dock = DockStyle.Fill, BackColor = ThemeSurface };

            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = ThemeSurface,
                Padding = new Padding(4, 2, 4, 10)
            };

            var badge = new Label
            {
                Dock = DockStyle.Right,
                Width = 90,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = total.ToString("N0"),
                ForeColor = ThemeAccent,
                BackColor = Color.FromArgb(248, 228, 233),
                Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
                Margin = new Padding(0)
            };

            var lblTitulo = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = ThemeText,
                Text = titulo
            };
            var lblSub = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                ForeColor = ThemeMuted,
                Text = subtitulo,
                AutoEllipsis = true
            };

            top.Controls.Add(badge);
            top.Controls.Add(lblSub);
            top.Controls.Add(lblTitulo);
            shell.Controls.Add(top);
            return shell;
        }

        private void ActivateView(string key)
        {
            if (_activeKey == key) return;
            _activeKey = key;
            _contentHost.Controls.Clear();
            _contentHost.Controls.Add(_views[key]);
            foreach (var card in _cards)
            {
                card.Value.Invalidate();
            }
        }

        private static DataGridView BaseGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                BackgroundColor = ThemeSurface,
                BorderStyle = BorderStyle.None,
                GridColor = Color.Gainsboro,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ThemeAccent,
                    ForeColor = Color.White,
                    SelectionBackColor = ThemeAccent,
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ThemeSurface,
                    ForeColor = ThemeText,
                    SelectionBackColor = ThemeAccent,
                    SelectionForeColor = Color.White,
                    WrapMode = DataGridViewTriState.True
                },
                RowTemplate = { Height = 34 }
            };
        }

        private static DataGridViewTextBoxColumn TextCol(string prop, string header, int width, bool fill = false)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                HeaderText = header,
                Width = fill ? 100 : width,
                AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
            };
        }
    }
}
