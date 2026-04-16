using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GastroSAE
{
    public class FormIngredientesConfig : Form
    {
        private readonly DataGridView dgv;
        private readonly Button btnAgregar, btnEditar, btnEliminar, btnCerrar;
        private readonly BindingList<SaeCatalogAdmin.IngredienteDto> _ingredientes = new();

        public FormIngredientesConfig()
        {
            Text = "Ingredientes (BD SAE)";
            StartPosition = FormStartPosition.CenterParent;
            Width = 860;
            Height = 560;
            MinimumSize = new Size(740, 440);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false,
                DataSource = _ingredientes
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Clave", DataPropertyName = "Clave", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripción", DataPropertyName = "Descripcion", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unidad", DataPropertyName = "Unidad", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tipo", DataPropertyName = "TipoElemento", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Exist", DataPropertyName = "Existencia", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stock mín", DataPropertyName = "StockMinimo", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stock máx", DataPropertyName = "StockMaximo", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 8 });

            btnAgregar = new Button { Text = "Agregar", MinimumSize = new Size(120, 44) };
            btnEditar = new Button { Text = "Editar", MinimumSize = new Size(120, 44) };
            btnEliminar = new Button { Text = "Eliminar", MinimumSize = new Size(120, 44) };
            btnCerrar = new Button { Text = "Cerrar", MinimumSize = new Size(120, 44) };

            btnAgregar.Click += (_, __) => Agregar();
            btnEditar.Click += (_, __) => Editar();
            btnEliminar.Click += (_, __) => Eliminar();
            btnCerrar.Click += (_, __) => Close();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var flOps = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0)
            };
            flOps.Controls.Add(btnAgregar);
            flOps.Controls.Add(btnEditar);
            flOps.Controls.Add(btnEliminar);

            var flClose = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0)
            };
            flClose.Controls.Add(btnCerrar);

            bottom.Controls.Add(flOps, 0, 0);
            bottom.Controls.Add(flClose, 1, 0);
            root.Controls.Add(dgv, 0, 0);
            root.Controls.Add(bottom, 0, 1);
            Controls.Add(root);

            Load += (_, __) => Cargar();
            UiStyle.Apply(this);
            UiFields.Apply(this);
            CancelButton = btnCerrar;

            UiHints.Attach(this, new (Control control, string hint)[]
            {
                (btnAgregar, "Ins"),
                (btnEditar, "Enter"),
                (btnEliminar, "Del"),
                (btnCerrar, "Esc"),
            });

            KeyPreview = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Insert) { Agregar(); return true; }
            if (keyData == Keys.Enter && dgv.Focused) { Editar(); return true; }
            if (keyData == Keys.Delete && dgv.Focused) { Eliminar(); return true; }
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private SaeCatalogAdmin.IngredienteDto? Seleccionado()
            => dgv.CurrentRow?.DataBoundItem as SaeCatalogAdmin.IngredienteDto;

        private void Cargar()
        {
            try
            {
                _ingredientes.Clear();
                var lista = SaeCatalogAdmin.ListarIngredientes();
                foreach (var item in lista)
                    _ingredientes.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudieron cargar los ingredientes.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Agregar()
        {
            using var f = new IngredienteDialog();
            if (f.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                SaeCatalogAdmin.InsertIngrediente(f.Clave, f.Descripcion, f.Unidad, f.StockMinimo, f.StockMaximo);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo agregar el ingrediente.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Editar()
        {
            var item = Seleccionado();
            if (item == null) return;

            using var f = new IngredienteDialog(item.Clave, item.Descripcion, item.Unidad, item.StockMinimo, item.StockMaximo, modoEdicion: true);
            if (f.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                SaeCatalogAdmin.UpdateIngrediente(item.Clave, f.Descripcion, f.Unidad, f.StockMinimo, f.StockMaximo);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo actualizar el ingrediente.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Eliminar()
        {
            var item = Seleccionado();
            if (item == null) return;
            if (MessageBox.Show(this,
                    $"¿Dar de baja el ingrediente '{item.Descripcion}'?",
                    "Confirmar",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                SaeCatalogAdmin.BajaIngrediente(item.Clave);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo dar de baja el ingrediente.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private sealed class IngredienteDialog : Form
        {
            private readonly TextBox txtClave = new();
            private readonly TextBox txtDescripcion = new();
            private readonly ComboBox cboUnidad = new();
            private readonly Label lblDetalleUnidad = new();
            private readonly NumericUpDown nudStockMin = new();
            private readonly NumericUpDown nudStockMax = new();
            private bool _stockMinInicializando = true;
            private bool _stockMinEditadoManual = false;
            private readonly Button btnOk = new();
            private readonly Button btnCancel = new();

            public string Clave => (txtClave.Text ?? string.Empty).Trim();
            public string Descripcion => (txtDescripcion.Text ?? string.Empty).Trim();
            public string Unidad => (cboUnidad.SelectedItem as SaeCatalogAdmin.UnitProfile)?.Key
                                     ?? (cboUnidad.Text ?? string.Empty).Trim().ToLowerInvariant();
            public decimal StockMinimo => nudStockMin.Value;
            public decimal StockMaximo => nudStockMax.Value;

            public IngredienteDialog(string? clave = null, string? descripcion = null, string? unidad = null, decimal? stockMinimo = null, decimal? stockMaximo = null, bool modoEdicion = false)
            {
                Text = modoEdicion ? "Editar ingrediente (SAE)" : "Nuevo ingrediente (SAE)";
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(560, 300);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                txtClave.MaxLength = 16;
                txtDescripcion.MaxLength = 40;
                cboUnidad.DropDownStyle = ComboBoxStyle.DropDownList;
                cboUnidad.DisplayMember = "DisplayName";
                cboUnidad.ValueMember = "Key";
                cboUnidad.Items.AddRange(SaeCatalogAdmin.GetUnitProfiles().ToArray());

                txtClave.Text = clave ?? string.Empty;
                txtDescripcion.Text = descripcion ?? string.Empty;
                txtClave.ReadOnly = modoEdicion;
                txtClave.TabStop = !modoEdicion;

                btnOk.Text = modoEdicion ? "Guardar" : "Crear";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.MinimumSize = new Size(110, 36);
                btnCancel.Text = "Cancelar";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.MinimumSize = new Size(110, 36);

                nudStockMin.DecimalPlaces = 3;
                nudStockMin.Minimum = 0;
                nudStockMin.Maximum = 1000000;
                nudStockMin.ThousandsSeparator = true;
                nudStockMin.Dock = DockStyle.Left;
                nudStockMin.Width = 180;
                nudStockMin.Margin = new Padding(0, 6, 0, 0);
                nudStockMin.ValueChanged += (_, __) =>
                {
                    if (!_stockMinInicializando)
                        _stockMinEditadoManual = true;
                };

                nudStockMax.DecimalPlaces = 3;
                nudStockMax.Minimum = 0;
                nudStockMax.Maximum = 1000000;
                nudStockMax.ThousandsSeparator = true;
                nudStockMax.Dock = DockStyle.Left;
                nudStockMax.Width = 180;
                nudStockMax.Margin = new Padding(0, 6, 0, 0);

                btnOk.Click += BtnOk_Click;

                var tl = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 7,
                    Padding = new Padding(18, 16, 18, 16),
                    BackColor = Color.Transparent
                };
                tl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
                tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
                tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var lblClave = new Label { Text = "Clave:", AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 10, 10, 0) };
                var lblDesc = new Label { Text = "Descripción:", AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 10, 10, 0) };
                var lblUnidad = new Label { Text = "Unidad:", AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 10, 10, 0) };
                var lblStockMin = new Label { Text = "Stock mín:", AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 10, 10, 0) };
                var lblStockMax = new Label { Text = "Stock máx:", AutoSize = true, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 10, 10, 0) };

                txtClave.Dock = DockStyle.Fill;
                txtClave.Margin = new Padding(0, 6, 0, 0);
                txtDescripcion.Dock = DockStyle.Fill;
                txtDescripcion.Margin = new Padding(0, 6, 0, 0);
                cboUnidad.Width = 220;
                cboUnidad.Margin = new Padding(0, 6, 0, 0);
                lblDetalleUnidad.AutoSize = true;
                lblDetalleUnidad.Margin = new Padding(0, 0, 0, 0);
                lblDetalleUnidad.ForeColor = Color.DimGray;
                cboUnidad.SelectedIndexChanged += (_, __) => ActualizarDetalleUnidad();

                var flButtons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    Padding = new Padding(0, 10, 0, 0),
                    Margin = new Padding(0)
                };
                flButtons.Controls.Add(btnCancel);
                flButtons.Controls.Add(btnOk);

                tl.Controls.Add(lblClave, 0, 0);
                tl.Controls.Add(txtClave, 1, 0);
                tl.Controls.Add(lblDesc, 0, 1);
                tl.Controls.Add(txtDescripcion, 1, 1);
                tl.Controls.Add(lblUnidad, 0, 2);
                tl.Controls.Add(cboUnidad, 1, 2);
                tl.Controls.Add(lblStockMin, 0, 3);
                tl.Controls.Add(nudStockMin, 1, 3);
                tl.Controls.Add(lblStockMax, 0, 4);
                tl.Controls.Add(nudStockMax, 1, 4);
                tl.Controls.Add(lblDetalleUnidad, 1, 5);
                tl.Controls.Add(flButtons, 1, 6);

                Controls.Add(tl);
                AcceptButton = btnOk;
                CancelButton = btnCancel;

                UiStyle.Apply(this);
                UiFields.Apply(this);

                SeleccionarUnidadInicial(unidad);
                _stockMinInicializando = true;
                var stockMinSugerido = stockMinimo ?? SaeCatalogAdmin.GetSuggestedStockMin(Unidad);
                nudStockMin.Value = Math.Min(nudStockMin.Maximum, Math.Max(nudStockMin.Minimum, stockMinSugerido));
                nudStockMax.Value = Math.Min(nudStockMax.Maximum, Math.Max(nudStockMax.Minimum, stockMaximo ?? Math.Max(stockMinSugerido, 50m)));
                _stockMinInicializando = false;
                _stockMinEditadoManual = stockMinimo.HasValue;
                ActualizarDetalleUnidad();
            }

            private void SeleccionarUnidadInicial(string? unidad)
            {
                var profile = SaeCatalogAdmin.ResolveUnitProfile(unidad);
                for (int i = 0; i < cboUnidad.Items.Count; i++)
                {
                    if (cboUnidad.Items[i] is SaeCatalogAdmin.UnitProfile p && p.Key == profile.Key)
                    {
                        cboUnidad.SelectedIndex = i;
                        return;
                    }
                }
                if (cboUnidad.Items.Count > 0)
                    cboUnidad.SelectedIndex = 0;
            }

            private void ActualizarDetalleUnidad()
            {
                if (cboUnidad.SelectedItem is SaeCatalogAdmin.UnitProfile p)
                {
                    lblDetalleUnidad.Text = $"Entrada en {p.UniMed} • Salida en {p.UniAlt}";
                    if (!_stockMinEditadoManual)
                    {
                        var sugerido = SaeCatalogAdmin.GetSuggestedStockMin(p.UniAlt);
                        _stockMinInicializando = true;
                        nudStockMin.Value = Math.Min(nudStockMin.Maximum, Math.Max(nudStockMin.Minimum, sugerido));
                        _stockMinInicializando = false;
                    }
                }
                else
                    lblDetalleUnidad.Text = string.Empty;
            }

            private void BtnOk_Click(object? sender, EventArgs e)
            {
                var clave = Clave;
                var descr = Descripcion;
                var unidad = Unidad;
                var stockMin = StockMinimo;
                var stockMax = StockMaximo;

                if (string.IsNullOrWhiteSpace(clave))
                {
                    MessageBox.Show(this, "La clave del ingrediente es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtClave.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (clave.Length > 16)
                {
                    MessageBox.Show(this, "La clave no puede exceder 16 caracteres.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtClave.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (string.IsNullOrWhiteSpace(descr))
                {
                    MessageBox.Show(this, "La descripción es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDescripcion.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (descr.Length > 40)
                {
                    MessageBox.Show(this, "La descripción no puede exceder 40 caracteres.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDescripcion.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (string.IsNullOrWhiteSpace(unidad))
                {
                    MessageBox.Show(this, "La unidad es obligatoria.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboUnidad.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (stockMin < 0)
                {
                    MessageBox.Show(this, "El stock mínimo no puede ser menor que cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudStockMin.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (stockMax < 0)
                {
                    MessageBox.Show(this, "El stock máximo no puede ser menor que cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudStockMax.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
                if (stockMax > 0 && stockMax < stockMin)
                {
                    MessageBox.Show(this, "El stock máximo no puede ser menor que el stock mínimo.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudStockMax.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }
            }
        }
    }
}
