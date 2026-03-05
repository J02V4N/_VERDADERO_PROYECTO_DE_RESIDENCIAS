using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    /// <summary>
    /// Refactor de layout (runtime) para hacer las pantallas realmente adaptables:
    /// - Reacomoda los controles EXISTENTES dentro de TableLayoutPanel / SplitContainer.
    /// - No toca la lógica ni los nombres, solo cambia el Parent/Dock/Anchor.
    /// - Evita encimados y hace que el resize funcione.
    ///
    /// Se aplica en runtime para no romper el Designer.
    /// </summary>
    public static class UiRefactorMain
    {
        public static void Apply(Form root)
        {
            if (root == null) return;

            // Solo una vez
            if (root.Tag is string s && s.Contains("UI_REFACTORED")) return;

            try
            {
                var tabMain = Find<TabControl>(root, "tabMain");
                if (tabMain == null) return;

                // Refactor por tabs (orden recomendado)
                var tabPedido = Find<TabPage>(root, "tabPedido");
                var tabCobro = Find<TabPage>(root, "tabCobro");
                var tabMesas = Find<TabPage>(root, "tabMesas");
                var tabInventario = Find<TabPage>(root, "tabInventario");
                var tabConfig = Find<TabPage>(root, "tabConfig");

                if (tabPedido != null) RefactorPedido(tabPedido);
                if (tabCobro != null) RefactorCobro(tabCobro);
                if (tabMesas != null) RefactorMesas(tabMesas);
                if (tabInventario != null) RefactorInventario(tabInventario);
                if (tabConfig != null) RefactorConfig(tabConfig);

                root.Tag = (root.Tag?.ToString() ?? "") + "|UI_REFACTORED";
            }
            catch
            {
                // Nunca romper la app por un layout.
            }
        }

        /// <summary>
        /// SplitContainer lanza ArgumentOutOfRange si SplitterDistance se asigna antes de que el control
        /// tenga un tamaño real (por ejemplo, recién creado con Width=150). Este helper aplica el
        /// SplitterDistance de forma segura una vez que el control ya fue layouted.
        /// </summary>
        private static void SafeInitSplitterDistance(SplitContainer sc, int desired)
        {
            if (sc == null) return;

            void apply()
            {
                try
                {
                    int w = sc.Width;
                    if (w <= 0) return;

                    int min = Math.Max(0, sc.Panel1MinSize);
                    int max = w - Math.Max(0, sc.Panel2MinSize) - sc.SplitterWidth;
                    if (max < min) return;

                    int val = Math.Min(Math.Max(desired, min), max);
                    sc.SplitterDistance = val;
                }
                catch
                {
                    // no-op
                }
            }

            apply();
            sc.SizeChanged += (s, e) => apply();
            sc.HandleCreated += (s, e) => apply();
        }

        private static void RefactorPedido(TabPage tab)
        {
            // Evitar doble refactor
            if (tab.Controls.OfType<TableLayoutPanel>().Any(t => t.Name == "tlpPedidoRoot")) return;

            var dgvReceta = Find<DataGridView>(tab, "dgvReceta");
            var btnQuitarLinea = Find<Button>(tab, "btnQuitarLinea");
            var label1 = Find<Label>(tab, "label1");
            var txtBuscarPlatillo = Find<TextBox>(tab, "txtBuscarPlatillo");
            var btnIrCobro = Find<Button>(tab, "btnIrCobro");
            var lblTotales = Find<Label>(tab, "lblTotales");
            var dgvPedido = Find<DataGridView>(tab, "dgvPedido");
            var btnAgregarLinea = Find<Button>(tab, "btnAgregarLinea");
            var lbPlatillos = Find<ListBox>(tab, "lbPlatillos");

            if (dgvReceta == null || btnQuitarLinea == null || txtBuscarPlatillo == null ||
                btnIrCobro == null || lblTotales == null || dgvPedido == null ||
                btnAgregarLinea == null || lbPlatillos == null)
                return;

            tab.SuspendLayout();
            var keep = tab.Controls.Cast<Control>().ToArray();

            try
            {
                tab.Controls.Clear();
                // En Pedido, el scroll se siente “buggy” y deja espacios raros.
                tab.AutoScroll = false;

                // Root: Catálogo (izq) + Trabajo (der)
                var root = new TableLayoutPanel
                {
                    Name = "tlpPedidoRoot",
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = new Padding(12),
                    BackColor = Color.Transparent
                };
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                // ===== Izquierda: búsqueda + lista + acción Agregar =====
                var left = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    BackColor = Color.Transparent
                };
                left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                left.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                if (label1 != null) label1.Visible = false; // placeholder ya explica

                txtBuscarPlatillo.Dock = DockStyle.Top;
                txtBuscarPlatillo.Margin = new Padding(0, 0, 0, 10);

                lbPlatillos.Dock = DockStyle.Fill;
                lbPlatillos.Margin = new Padding(0, 0, 0, 10);

                btnAgregarLinea.Text = "Agregar";
                btnAgregarLinea.Dock = DockStyle.Top;
                btnAgregarLinea.Margin = new Padding(0);

                left.Controls.Add(txtBuscarPlatillo, 0, 0);
                left.Controls.Add(lbPlatillos, 0, 1);
                left.Controls.Add(btnAgregarLinea, 0, 2);

                // ===== Derecha: barra + grids (Pedido | Receta/Insumos) =====
                var right = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.Transparent
                };
                right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var bar = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 4,
                    RowCount = 1,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                btnQuitarLinea.Text = "Quitar";
                btnQuitarLinea.Dock = DockStyle.Fill;
                btnQuitarLinea.Margin = new Padding(0, 0, 12, 0);

                lblTotales.AutoSize = true;
                lblTotales.Anchor = AnchorStyles.Right;
                lblTotales.Margin = new Padding(0, 0, 12, 0);

                btnIrCobro.Dock = DockStyle.Fill;
                btnIrCobro.Margin = new Padding(0);

                bar.Controls.Add(btnQuitarLinea, 0, 0);
                bar.Controls.Add(new Panel { Dock = DockStyle.Fill }, 1, 0);
                bar.Controls.Add(lblTotales, 2, 0);
                bar.Controls.Add(btnIrCobro, 3, 0);

                var grids = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    BackColor = Color.Transparent
                };
                grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
                grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
                grids.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                dgvPedido.Dock = DockStyle.Fill;
                dgvPedido.Margin = new Padding(0, 10, 10, 0);

                dgvReceta.Dock = DockStyle.Fill;
                dgvReceta.Margin = new Padding(0, 10, 0, 0);

                // Ajuste visual para que las columnas no se “salgan” del área asignada.
                // (Fill + pesos + wrap en descripción)
                ConfigurePedidoGrids(dgvPedido, dgvReceta);

                grids.Controls.Add(dgvPedido, 0, 0);
                grids.Controls.Add(dgvReceta, 1, 0);

                right.Controls.Add(bar, 0, 0);
                right.Controls.Add(grids, 0, 1);

                root.Controls.Add(left, 0, 0);
                root.Controls.Add(right, 1, 0);

                tab.Controls.Add(root);
                tab.ResumeLayout(true);
            }
            catch
            {
                tab.Controls.Clear();
                tab.Controls.AddRange(keep);
                tab.ResumeLayout(true);
            }
        }

        private static void ConfigurePedidoGrids(DataGridView dgvPedido, DataGridView dgvReceta)
        {
            // Pedido
            try
            {
                dgvPedido.SuspendLayout();
                dgvPedido.RowHeadersVisible = false;
                dgvPedido.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvPedido.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgvPedido.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgvPedido.ColumnHeadersHeight = Math.Max(dgvPedido.ColumnHeadersHeight, 34);
                dgvPedido.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

                foreach (DataGridViewColumn c in dgvPedido.Columns)
                {
                    c.MinimumWidth = 50;
                    // Descripción tiende a ser el campo más largo.
                    if (c.Name.Equals("Descripcion", StringComparison.OrdinalIgnoreCase) ||
                        c.HeaderText.IndexOf("Descr", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.FillWeight = 42;
                        c.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }
                    else if (c.Name.Equals("Clave", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 16;
                    }
                    else if (c.Name.Equals("Cant", StringComparison.OrdinalIgnoreCase) || c.HeaderText.Contains("Cant", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 10;
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.DefaultCellStyle.Format = "0.##";
                    }
                    else if (c.HeaderText.Contains("Peso", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 10;
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.DefaultCellStyle.Format = "0.##";
                    }
                    else if (c.HeaderText.Contains("P.Unit", StringComparison.OrdinalIgnoreCase) || c.HeaderText.Contains("Unit", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 12;
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.DefaultCellStyle.Format = "0.00";
                    }
                    else if (c.HeaderText == "#" || c.Name.Equals("Num", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 6;
                        c.MinimumWidth = 45;
                    }
                    else
                    {
                        c.FillWeight = 10;
                    }
                }
                dgvPedido.ResumeLayout(true);
            }
            catch
            {
                // no-op
            }

            // Receta / Insumos
            try
            {
                dgvReceta.SuspendLayout();
                dgvReceta.RowHeadersVisible = false;
                dgvReceta.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvReceta.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                dgvReceta.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                dgvReceta.ColumnHeadersHeight = Math.Max(dgvReceta.ColumnHeadersHeight, 34);
                dgvReceta.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

                foreach (DataGridViewColumn c in dgvReceta.Columns)
                {
                    c.MinimumWidth = 55;
                    if (c.Name.Equals("Insumo", StringComparison.OrdinalIgnoreCase) || c.HeaderText.Contains("Insum", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 20;
                    }
                    else if (c.Name.Equals("Descripcion", StringComparison.OrdinalIgnoreCase) || c.HeaderText.IndexOf("Descr", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.FillWeight = 40;
                        c.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }
                    else if (c.HeaderText.Equals("UM", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 10;
                    }
                    else if (c.HeaderText.Contains("Cant", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 15;
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.DefaultCellStyle.Format = "0.###";
                    }
                    else if (c.HeaderText.Contains("Exist", StringComparison.OrdinalIgnoreCase))
                    {
                        c.FillWeight = 15;
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        c.DefaultCellStyle.Format = "0.###";
                    }
                    else
                    {
                        c.FillWeight = 10;
                    }
                }
                dgvReceta.ResumeLayout(true);
            }
            catch
            {
                // no-op
            }
        }

        private static void RefactorCobro(TabPage tab)
        {
            if (tab.Controls.OfType<TableLayoutPanel>().Any(t => t.Name == "tlpCobroRoot")) return;

            var btnCobroCancelar = Find<Button>(tab, "btnCobroCancelar");
            var btnCobroConfirmar = Find<Button>(tab, "btnCobroConfirmar");
            var lblCobroCambio = Find<Label>(tab, "lblCobroCambio");
            var txtCobroRef = Find<TextBox>(tab, "txtCobroRef");
            var txtCobroTarjeta = Find<TextBox>(tab, "txtCobroTarjeta");
            var txtCobroEfectivo = Find<TextBox>(tab, "txtCobroEfectivo");
            var lblCobroTotal = Find<Label>(tab, "lblCobroTotal");
            var lblCobroMesa = Find<Label>(tab, "lblCobroMesa");
            var btnReimprimir = Find<Button>(tab, "btnReimprimir");
            var lblCambio = Find<Label>(tab, "lblCambio");
            var txtImporteRecibido = Find<TextBox>(tab, "txtImporteRecibido");
            var cboFormaPago = Find<ComboBox>(tab, "cboFormaPago");
            var cboMetodoPago = Find<ComboBox>(tab, "cboMetodoPago");
            var lblResumenCobro = Find<Label>(tab, "lblResumenCobro");
            var btnConfirmarCobro = Find<Button>(tab, "btnConfirmarCobro");
            var cboUsoCFDI = Find<ComboBox>(tab, "cboUsoCFDI");
            var txtRazon = Find<TextBox>(tab, "txtRazon");
            var txtRFC = Find<TextBox>(tab, "txtRFC");
            var chkFacturarAhora = Find<CheckBox>(tab, "chkFacturarAhora");

            if (btnCobroCancelar == null || btnCobroConfirmar == null || lblCobroCambio == null ||
                txtCobroRef == null || txtCobroTarjeta == null || txtCobroEfectivo == null ||
                lblCobroTotal == null || lblCobroMesa == null || btnReimprimir == null ||
                lblCambio == null || txtImporteRecibido == null || cboFormaPago == null ||
                cboMetodoPago == null || lblResumenCobro == null || btnConfirmarCobro == null ||
                cboUsoCFDI == null || txtRazon == null || txtRFC == null || chkFacturarAhora == null)
                return;

            tab.SuspendLayout();
            tab.Controls.Clear();

            var root = new TableLayoutPanel
            {
                Name = "tlpCobroRoot",
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Header
            var hdr = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            hdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            hdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            lblCobroMesa.AutoSize = true;
            lblCobroMesa.Anchor = AnchorStyles.Left;
            lblCobroMesa.Margin = new Padding(0, 0, 0, 6);

            lblCobroTotal.AutoSize = true;
            lblCobroTotal.Anchor = AnchorStyles.Right;
            lblCobroTotal.Margin = new Padding(0, 0, 0, 6);

            hdr.Controls.Add(lblCobroMesa, 0, 0);
            hdr.Controls.Add(lblCobroTotal, 1, 0);

            root.Controls.Add(hdr, 0, 0);

            // Body: 2 columnas
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            // Izquierda: Pago
            var gbPago = new GroupBox
            {
                Text = "Pago",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var tlPago = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.Transparent
            };
            tlPago.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // resumen
            tlPago.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // forma
            tlPago.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // método
            tlPago.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // importe recibido
            tlPago.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // cambio
            tlPago.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // spacer
            tlPago.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // cobro rapido group

            lblResumenCobro.AutoSize = true;
            lblResumenCobro.Margin = new Padding(0, 0, 0, 10);

            cboFormaPago.Dock = DockStyle.Top;
            cboFormaPago.Margin = new Padding(0, 0, 0, 10);

            cboMetodoPago.Dock = DockStyle.Top;
            cboMetodoPago.Margin = new Padding(0, 0, 0, 10);

            txtImporteRecibido.Dock = DockStyle.Top;
            txtImporteRecibido.Margin = new Padding(0, 0, 0, 6);

            lblCambio.AutoSize = true;
            lblCambio.Margin = new Padding(0, 0, 0, 10);

            tlPago.Controls.Add(lblResumenCobro, 0, 0);
            tlPago.Controls.Add(cboFormaPago, 0, 1);
            tlPago.Controls.Add(cboMetodoPago, 0, 2);
            tlPago.Controls.Add(txtImporteRecibido, 0, 3);
            tlPago.Controls.Add(lblCambio, 0, 4);
            tlPago.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 5);

            // Cobro rápido (opcional): mantenemos componentes existentes, pero ordenados.
            var gbRapido = new GroupBox
            {
                Text = "Cobro rápido (opcional)",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true
            };

            var tlRapido = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 3,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            tlRapido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tlRapido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            txtCobroEfectivo.Dock = DockStyle.Top;
            txtCobroTarjeta.Dock = DockStyle.Top;
            txtCobroRef.Dock = DockStyle.Top;

            txtCobroEfectivo.Margin = new Padding(0, 0, 8, 8);
            txtCobroTarjeta.Margin = new Padding(0, 0, 0, 8);
            txtCobroRef.Margin = new Padding(0, 0, 0, 8);

            lblCobroCambio.AutoSize = true;
            lblCobroCambio.Anchor = AnchorStyles.Right;
            lblCobroCambio.Margin = new Padding(0, 6, 0, 0);

            btnCobroConfirmar.Text = "Confirmar (rápido)";
            btnCobroConfirmar.Dock = DockStyle.Right;

            tlRapido.Controls.Add(txtCobroEfectivo, 0, 0);
            tlRapido.Controls.Add(txtCobroTarjeta, 1, 0);
            tlRapido.Controls.Add(txtCobroRef, 0, 1);
            tlRapido.SetColumnSpan(txtCobroRef, 2);

            var rapidoFooter = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            rapidoFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            rapidoFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            rapidoFooter.Controls.Add(lblCobroCambio, 0, 0);
            rapidoFooter.Controls.Add(btnCobroConfirmar, 1, 0);

            gbRapido.Controls.Add(rapidoFooter);
            gbRapido.Controls.Add(tlRapido);

            tlPago.Controls.Add(gbRapido, 0, 6);

            gbPago.Controls.Add(tlPago);

            // Derecha: CFDI
            var gbCfdi = new GroupBox
            {
                Text = "CFDI (opcional)",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var tlCfdi = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            tlCfdi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlCfdi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlCfdi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlCfdi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlCfdi.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            chkFacturarAhora.Margin = new Padding(0, 0, 0, 10);
            txtRFC.Dock = DockStyle.Top;
            txtRFC.Margin = new Padding(0, 0, 0, 10);
            txtRazon.Dock = DockStyle.Top;
            txtRazon.Margin = new Padding(0, 0, 0, 10);
            cboUsoCFDI.Dock = DockStyle.Top;
            cboUsoCFDI.Margin = new Padding(0, 0, 0, 10);

            tlCfdi.Controls.Add(chkFacturarAhora, 0, 0);
            tlCfdi.Controls.Add(txtRFC, 0, 1);
            tlCfdi.Controls.Add(txtRazon, 0, 2);
            tlCfdi.Controls.Add(cboUsoCFDI, 0, 3);
            tlCfdi.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 4);

            gbCfdi.Controls.Add(tlCfdi);

            body.Controls.Add(gbPago, 0, 0);
            body.Controls.Add(gbCfdi, 1, 0);

            root.Controls.Add(body, 0, 1);

            // Actions
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                ColumnCount = 5,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            btnCobroCancelar.Dock = DockStyle.Left;
            btnReimprimir.Dock = DockStyle.Right;
            btnConfirmarCobro.Dock = DockStyle.Right;

            btnCobroCancelar.Margin = new Padding(0, 10, 10, 0);
            btnReimprimir.Margin = new Padding(0, 10, 10, 0);
            btnConfirmarCobro.Margin = new Padding(0, 10, 0, 0);

            // (Opcional) un hueco flexible
            actions.Controls.Add(btnCobroCancelar, 0, 0);
            actions.Controls.Add(new Panel { Dock = DockStyle.Fill }, 1, 0);
            actions.Controls.Add(btnReimprimir, 3, 0);
            actions.Controls.Add(btnConfirmarCobro, 4, 0);

            root.Controls.Add(actions, 0, 2);

            tab.Controls.Add(root);
            tab.ResumeLayout(true);
        }

        private static void RefactorMesas(TabPage tab)
        {
            if (tab.Controls.OfType<TableLayoutPanel>().Any(t => t.Name == "tlpMesasRoot")) return;

            var statusMain = Find<StatusStrip>(tab, "statusMain");
            var dgvMesas = Find<DataGridView>(tab, "dgvMesas");
            var panel1 = Find<Panel>(tab, "panel1");

            if (statusMain == null || dgvMesas == null || panel1 == null) return;

            var btnLiberarMesa = Find<Button>(panel1, "btnLiberarMesa");
            var btnAsignarMesero = Find<Button>(panel1, "btnAsignarMesero");
            var btnAbrirMesa = Find<Button>(panel1, "btnAbrirMesa");
            var lblMesaSel = Find<Label>(panel1, "lblMesaSel");
            var cboMesero = Find<ComboBox>(panel1, "cboMesero");

            if (btnLiberarMesa == null || btnAsignarMesero == null || btnAbrirMesa == null || lblMesaSel == null || cboMesero == null)
                return;

            tab.SuspendLayout();
            var keep = tab.Controls.Cast<Control>().ToArray();

            try
            {
                tab.Controls.Clear();
                // Evita scrollbars que arruinan la composición visual.
                tab.AutoScroll = false;

                var root = new TableLayoutPanel
                {
                    Name = "tlpMesasRoot",
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    Padding = new Padding(12),
                    BackColor = Color.Transparent
                };
                // Columna izquierda: se ajusta al ancho real del grid (sin “comerse” media pantalla).
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                // Izquierda: mesas (grid grande)
                dgvMesas.Dock = DockStyle.Fill;
                dgvMesas.Margin = new Padding(0, 0, 10, 0);
                ConfigureMesasGridAndWidth(dgvMesas, root);
                root.Controls.Add(dgvMesas, 0, 0);

                // Derecha: tarjeta de acciones
                var gb = new GroupBox
                {
                    Text = "Acciones",
                    Dock = DockStyle.Fill,
                    Padding = new Padding(12)
                };

                var actions = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 5,
                    BackColor = Color.Transparent
                };
                actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                lblMesaSel.AutoSize = true;
                lblMesaSel.Margin = new Padding(0, 0, 0, 10);

                var assignRow = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    RowCount = 1,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                assignRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
                assignRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

                cboMesero.Dock = DockStyle.Fill;
                cboMesero.Margin = new Padding(0, 0, 10, 0);
                btnAsignarMesero.Dock = DockStyle.Fill;
                assignRow.Controls.Add(cboMesero, 0, 0);
                assignRow.Controls.Add(btnAsignarMesero, 1, 0);

                // Acciones primarias: apiladas y grandes
                btnAbrirMesa.Dock = DockStyle.Fill;
                btnLiberarMesa.Dock = DockStyle.Fill;

                btnAbrirMesa.Margin = new Padding(0, 12, 0, 10);
                btnLiberarMesa.Margin = new Padding(0, 0, 0, 0);

                actions.Controls.Add(lblMesaSel, 0, 0);
                actions.Controls.Add(assignRow, 0, 1);
                actions.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 2);
                actions.Controls.Add(btnAbrirMesa, 0, 3);
                actions.Controls.Add(btnLiberarMesa, 0, 4);

                gb.Controls.Add(actions);
                root.Controls.Add(gb, 1, 0);

                statusMain.Dock = DockStyle.Bottom;
                root.Controls.Add(statusMain, 0, 1);
                root.SetColumnSpan(statusMain, 2);

                tab.Controls.Add(root);
                tab.ResumeLayout(true);
            }
            catch
            {
                tab.Controls.Clear();
                tab.Controls.AddRange(keep);
                tab.ResumeLayout(true);
            }
        }

        private static void ConfigureMesasGridAndWidth(DataGridView dgvMesas, TableLayoutPanel root)
        {
            try
            {
                // Ancho preferido por contenido para no dejar un panel gigantesco cuando hay pocas mesas.
                dgvMesas.RowHeadersVisible = false;
                dgvMesas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dgvMesas.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                int w = 0;
                foreach (DataGridViewColumn c in dgvMesas.Columns)
                    w += c.Width;

                // Padding/margen
                w += 40;

                // Clamp razonable
                w = Math.Min(Math.Max(w, 380), 620);

                if (root.ColumnStyles.Count >= 2)
                {
                    root.ColumnStyles[0].SizeType = SizeType.Absolute;
                    root.ColumnStyles[0].Width = w;
                    root.ColumnStyles[1].SizeType = SizeType.Percent;
                    root.ColumnStyles[1].Width = 100;
                }

                // Mantener por contenido (más “refinado” para pocas filas/mesas).
                dgvMesas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            }
            catch
            {
                // no-op
            }
        }

                private static void RefactorInventario(TabPage tab)
        {
            // Evitar doble refactor
            if (tab.Controls.OfType<Panel>().Any(p => p.Name == "pInvRoot")) return;

            var btnInvLimpiar = Find<Button>(tab, "btnInvLimpiar");
            var btnInvEliminar = Find<Button>(tab, "btnInvEliminar");
            var btnInvGuardarAux = Find<Button>(tab, "btnInvGuardarAux");
            var lblInvTotales = Find<Label>(tab, "lblInvTotales");
            var dgvInvCaptura = Find<DataGridView>(tab, "dgvInvCaptura");
            var btnInvAgregar = Find<Button>(tab, "btnInvAgregar");
            var txtInvCostoKg = Find<TextBox>(tab, "txtInvCostoKg");
            var lblInvKg = Find<Label>(tab, "lblInvKg");
            var txtInvPesoGr = Find<TextBox>(tab, "txtInvPesoGr");
            var chkInvSimularBascula = Find<CheckBox>(tab, "chkInvSimularBascula");
            var btnInvRefrescar = Find<Button>(tab, "btnInvRefrescar");
            var lbInvArticulos = Find<ListBox>(tab, "lbInvArticulos");
            var txtInvBuscar = Find<TextBox>(tab, "txtInvBuscar");

            if (btnInvLimpiar == null || btnInvEliminar == null || btnInvGuardarAux == null ||
                lblInvTotales == null || dgvInvCaptura == null || btnInvAgregar == null ||
                txtInvCostoKg == null || lblInvKg == null || txtInvPesoGr == null ||
                chkInvSimularBascula == null || btnInvRefrescar == null || lbInvArticulos == null ||
                txtInvBuscar == null)
                return;

            tab.SuspendLayout();
            var keep = tab.Controls.Cast<Control>().ToArray();

            try
            {
                // IMPORTANTE:
                // Antes ocultábamos todos los controles (Visible=false) y luego los re-parentábamos.
                // Eso dejaba los controles reubicados invisibles (y la pantalla se veía “vacía”).
                // Para evitarlo, limpiamos la tab y rearmamos el layout, reusando los mismos controles.
                tab.AutoScroll = false;

                tab.Controls.Clear();

                // Asegurar que los controles que vamos a reusar estén visibles.
                txtInvBuscar.Visible = true;
                btnInvRefrescar.Visible = true;
                lbInvArticulos.Visible = true;
                chkInvSimularBascula.Visible = true;
                txtInvPesoGr.Visible = true;
                lblInvKg.Visible = true;
                txtInvCostoKg.Visible = true;
                btnInvAgregar.Visible = true;
                dgvInvCaptura.Visible = true;
                lblInvTotales.Visible = true;
                btnInvEliminar.Visible = true;
                btnInvLimpiar.Visible = true;
                btnInvGuardarAux.Visible = true;

                var pRoot = new Panel
                {
                    Name = "pInvRoot",
                    Dock = DockStyle.Fill,
                    Padding = new Padding(12),
                    BackColor = Color.Transparent
                };
                tab.Controls.Add(pRoot);

                var pLeft = new Panel
                {
                    Name = "pInvLeft",
                    Dock = DockStyle.Left,
                    Width = Math.Min(460, Math.Max(360, tab.Width / 3)),
                    BackColor = Color.Transparent
                };

                var pRight = new Panel
                {
                    Name = "pInvRight",
                    Dock = DockStyle.Fill,
                    Padding = new Padding(12, 0, 0, 0),
                    BackColor = Color.Transparent
                };

                pRoot.Controls.Add(pRight);
                pRoot.Controls.Add(pLeft);

                // LEFT: búsqueda + refrescar
                var pSearch = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 48,
                    BackColor = Color.Transparent
                };

                txtInvBuscar.Parent = pSearch;
                txtInvBuscar.Dock = DockStyle.Fill;
                txtInvBuscar.Margin = new Padding(0);

                btnInvRefrescar.Parent = pSearch;
                btnInvRefrescar.Text = "Refrescar";
                btnInvRefrescar.Dock = DockStyle.Right;
                btnInvRefrescar.Width = 140;

                pSearch.Controls.Add(txtInvBuscar);
                pSearch.Controls.Add(btnInvRefrescar);

                lbInvArticulos.Parent = pLeft;
                lbInvArticulos.Dock = DockStyle.Fill;
                lbInvArticulos.IntegralHeight = false;

                pLeft.Controls.Add(lbInvArticulos);
                pLeft.Controls.Add(pSearch);

                // RIGHT: Captura
                var gbCap = new GroupBox
                {
                    Text = "Captura",
                    Dock = DockStyle.Top,
                    Height = 140,
                    Padding = new Padding(12)
                };

                var tlCap = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 6,
                    RowCount = 2,
                    BackColor = Color.Transparent
                };
                tlCap.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlCap.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                tlCap.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // checkbox
                tlCap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140)); // peso
                tlCap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // kg label
                tlCap.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // costo/kg
                tlCap.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // agregar
                tlCap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // spacer

                chkInvSimularBascula.Margin = new Padding(0, 6, 12, 8);
                txtInvPesoGr.Margin = new Padding(0, 0, 12, 8);
                lblInvKg.Margin = new Padding(0, 6, 12, 8);
                txtInvCostoKg.Margin = new Padding(0, 0, 12, 8);
                btnInvAgregar.Margin = new Padding(0, 0, 12, 8);
                lblInvTotales.Margin = new Padding(0, 6, 0, 8);

                txtInvPesoGr.Dock = DockStyle.Fill;
                txtInvCostoKg.Dock = DockStyle.Fill;

                btnInvAgregar.Text = "Agregar";

                tlCap.Controls.Add(chkInvSimularBascula, 0, 0);
                tlCap.Controls.Add(txtInvPesoGr, 1, 0);
                tlCap.Controls.Add(lblInvKg, 2, 0);
                tlCap.Controls.Add(txtInvCostoKg, 3, 0);
                tlCap.Controls.Add(btnInvAgregar, 4, 0);

                tlCap.SetColumnSpan(lblInvTotales, 6);
                lblInvTotales.Anchor = AnchorStyles.Right;
                lblInvTotales.AutoSize = true;
                tlCap.Controls.Add(lblInvTotales, 0, 1);

                gbCap.Controls.Add(tlCap);

                // RIGHT: Acciones
                var pActions = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 56,
                    BackColor = Color.Transparent
                };

                btnInvEliminar.Text = "Eliminar";
                btnInvEliminar.Dock = DockStyle.Left;
                btnInvEliminar.Width = 140;

                btnInvLimpiar.Text = "Limpiar";
                btnInvLimpiar.Dock = DockStyle.Left;
                btnInvLimpiar.Width = 140;

                btnInvGuardarAux.Text = "Guardar";
                btnInvGuardarAux.Dock = DockStyle.Right;
                btnInvGuardarAux.Width = 160;

                pActions.Controls.Add(btnInvGuardarAux);
                pActions.Controls.Add(btnInvLimpiar);
                pActions.Controls.Add(btnInvEliminar);

                // RIGHT: Grid
                dgvInvCaptura.Dock = DockStyle.Fill;

                pRight.Controls.Add(dgvInvCaptura);
                pRight.Controls.Add(pActions);
                pRight.Controls.Add(gbCap);

                tab.ResumeLayout(true);
            }
            catch
            {
                tab.Controls.Clear();
                tab.Controls.AddRange(keep);
                foreach (var c in keep) c.Visible = true;
                tab.ResumeLayout(true);
            }
        }

private static void RefactorConfig(TabPage tab)
        {
            if (tab.Controls.OfType<TableLayoutPanel>().Any(t => t.Name == "tlpCfgRoot")) return;

            var btnCfgRecetas = Find<Button>(tab, "btnCfgRecetas");
            var btnCfgIngredientes = Find<Button>(tab, "btnCfgIngredientes");
            var btnCfgMeseros = Find<Button>(tab, "btnCfgMeseros");
            var btnCfgMesas = Find<Button>(tab, "btnCfgMesas");
            var btnGuardarConfig = Find<Button>(tab, "btnGuardarConfig");
            var btnProbarBascula = Find<Button>(tab, "btnProbarBascula");
            var cboListaPrecios = Find<ComboBox>(tab, "cboListaPrecios");
            var cboAlmacen = Find<ComboBox>(tab, "cboAlmacen");
            var txtPuertoCom = Find<TextBox>(tab, "txtPuertoCom");
            var cboImpresora = Find<ComboBox>(tab, "cboImpresora");
            var btnPruebaAux = Find<Button>(tab, "btnPruebaAux");
            var btnPruebaSae = Find<Button>(tab, "btnPruebaSae");
            var txtRutaAux = Find<TextBox>(tab, "txtRutaAux");
            var txtRutaSae = Find<TextBox>(tab, "txtRutaSae");

            if (btnCfgRecetas == null || btnCfgIngredientes == null || btnCfgMeseros == null || btnCfgMesas == null || btnGuardarConfig == null ||
                btnProbarBascula == null || cboListaPrecios == null || cboAlmacen == null || txtPuertoCom == null ||
                cboImpresora == null || btnPruebaAux == null || btnPruebaSae == null || txtRutaAux == null || txtRutaSae == null)
                return;

            tab.SuspendLayout();
            tab.Controls.Clear();

            var root = new TableLayoutPanel
            {
                Name = "tlpCfgRoot",
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Izquierda: Rutas y pruebas
            var gbConn = new GroupBox
            {
                Text = "Conexiones",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var tlConn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            tlConn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlConn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlConn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlConn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlConn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlConn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            txtRutaSae.Dock = DockStyle.Top;
            txtRutaAux.Dock = DockStyle.Top;
            btnPruebaSae.Dock = DockStyle.Fill;
            btnPruebaAux.Dock = DockStyle.Fill;

            txtRutaSae.Margin = new Padding(0, 0, 10, 10);
            btnPruebaSae.Margin = new Padding(0, 0, 0, 10);
            txtRutaAux.Margin = new Padding(0, 0, 10, 10);
            btnPruebaAux.Margin = new Padding(0, 0, 0, 10);

            tlConn.Controls.Add(txtRutaSae, 0, 0);
            tlConn.Controls.Add(btnPruebaSae, 1, 0);
            tlConn.Controls.Add(txtRutaAux, 0, 1);
            tlConn.Controls.Add(btnPruebaAux, 1, 1);

            // Spacer
            tlConn.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 3);
            tlConn.SetColumnSpan(tlConn.GetControlFromPosition(0, 3), 2);

            gbConn.Controls.Add(tlConn);

            // Derecha: parámetros
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            // Importante: "Administración" NO debe estirarse (se ve tosco) ni
            // recortarse (se pierde el 3er botón). Solución robusta:
            //  - Dispositivos: Auto
            //  - Catálogos: Auto
            //  - Administración: Auto (altura fija por contenido)
            //  - Filler: Percent (absorbe el espacio sobrante)
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var gbDevices = new GroupBox
            {
                Text = "Dispositivos",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true
            };
            var tlDev = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            cboImpresora.Dock = DockStyle.Top;
            txtPuertoCom.Dock = DockStyle.Top;
            btnProbarBascula.Dock = DockStyle.Top;

            cboImpresora.Margin = new Padding(0, 0, 0, 10);
            txtPuertoCom.Margin = new Padding(0, 0, 0, 10);
            btnProbarBascula.Margin = new Padding(0, 0, 0, 0);

            tlDev.Controls.Add(cboImpresora, 0, 0);
            tlDev.Controls.Add(txtPuertoCom, 0, 1);
            tlDev.Controls.Add(btnProbarBascula, 0, 2);
            gbDevices.Controls.Add(tlDev);

            var gbCatalog = new GroupBox
            {
                Text = "Catálogos",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true
            };
            var tlCat = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            cboAlmacen.Dock = DockStyle.Top;
            cboListaPrecios.Dock = DockStyle.Top;
            cboAlmacen.Margin = new Padding(0, 0, 0, 10);
            cboListaPrecios.Margin = new Padding(0, 0, 0, 0);
            tlCat.Controls.Add(cboAlmacen, 0, 0);
            tlCat.Controls.Add(cboListaPrecios, 0, 1);
            gbCatalog.Controls.Add(tlCat);

            var gbAdmin = new GroupBox
            {
                Text = "Administración",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Tabla interna: 4 botones en fila (compacta y sin recortes).
            var tlAdm = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            // Altura táctil (2 líneas: texto + atajo)
            const int ADM_BTN_H = 70;
            tlAdm.RowStyles.Add(new RowStyle(SizeType.Absolute, ADM_BTN_H));
            tlAdm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlAdm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlAdm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlAdm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            btnCfgMesas.Text = "Mesas";
            btnCfgMeseros.Text = "Meseros";
            btnCfgIngredientes.Text = "Ingredientes";
            btnCfgRecetas.Text = "Recetas";

            // Tamaño mínimo para touch.
            btnCfgMesas.MinimumSize = new System.Drawing.Size(0, ADM_BTN_H);
            btnCfgMeseros.MinimumSize = new System.Drawing.Size(0, ADM_BTN_H);
            btnCfgIngredientes.MinimumSize = new System.Drawing.Size(0, ADM_BTN_H);
            btnCfgRecetas.MinimumSize = new System.Drawing.Size(0, ADM_BTN_H);

            // Que llenen su celda.
            btnCfgMesas.Dock = DockStyle.Fill;
            btnCfgMeseros.Dock = DockStyle.Fill;
            btnCfgIngredientes.Dock = DockStyle.Fill;
            btnCfgRecetas.Dock = DockStyle.Fill;

            // Espaciado horizontal entre botones.
            btnCfgMesas.Margin = new Padding(0, 0, 10, 0);
            btnCfgMeseros.Margin = new Padding(0, 0, 10, 0);
            btnCfgIngredientes.Margin = new Padding(0, 0, 10, 0);
            btnCfgRecetas.Margin = new Padding(0);

            // Compacto: el grupo solo ocupa lo necesario.
            gbAdmin.MinimumSize = new System.Drawing.Size(0, ADM_BTN_H + 24);

            tlAdm.Controls.Add(btnCfgMesas, 0, 0);
            tlAdm.Controls.Add(btnCfgMeseros, 1, 0);
            tlAdm.Controls.Add(btnCfgIngredientes, 2, 0);
            tlAdm.Controls.Add(btnCfgRecetas, 3, 0);

            gbAdmin.Controls.Add(tlAdm);

            right.Controls.Add(gbDevices, 0, 0);
            right.Controls.Add(gbCatalog, 0, 1);
            right.Controls.Add(gbAdmin, 0, 2);
            right.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 3);

            // Acciones abajo
            btnGuardarConfig.Text = "Guardar";
            btnGuardarConfig.Dock = DockStyle.Right;
            btnGuardarConfig.Margin = new Padding(0, 10, 0, 0);

            root.Controls.Add(gbConn, 0, 0);
            root.Controls.Add(right, 1, 0);
            root.Controls.Add(btnGuardarConfig, 1, 1);

            tab.Controls.Add(root);
            tab.ResumeLayout(true);
        }

        private static T? Find<T>(Control root, string name) where T : Control
        {
            if (root == null) return null;
            if (root is T t && root.Name == name) return t;
            foreach (Control c in root.Controls)
            {
                var r = Find<T>(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
