using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    public class FormMeserosConfig : Form
    {
        private readonly DataGridView dgv;
        private readonly Button btnAgregar, btnEditar, btnEliminar, btnCerrar;

        private readonly TableLayoutPanel _root;

        private readonly BindingList<AuxRepo.MeseroDto> _meseros = new BindingList<AuxRepo.MeseroDto>();

        public FormMeserosConfig()
        {
            Text = "Meseros (BD Aux)";
            StartPosition = FormStartPosition.CenterParent;
            Width = 720;
            Height = 520;
            MinimumSize = new System.Drawing.Size(640, 420);
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
                DataSource = _meseros
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Id",
                DataPropertyName = "Id",
                Width = 60
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Nombre",
                DataPropertyName = "Nombre",
                Width = 260
            });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = "Activo",
                DataPropertyName = "Activo",
                Width = 80
            });

            btnAgregar = new Button { Text = "Agregar", MinimumSize = new System.Drawing.Size(120, 44) };
            btnEditar = new Button { Text = "Editar", MinimumSize = new System.Drawing.Size(120, 44) };
            btnEliminar = new Button { Text = "Eliminar", MinimumSize = new System.Drawing.Size(120, 44) };
            btnCerrar = new Button { Text = "Cerrar", MinimumSize = new System.Drawing.Size(120, 44) };

            btnAgregar.Click += (s, e) => Agregar();
            btnEditar.Click += (s, e) => Editar();
            btnEliminar.Click += (s, e) => Eliminar();
            btnCerrar.Click += (s, e) => Close();

            // ===== Layout: sin espacios muertos =====
            _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = System.Drawing.Color.Transparent
            };
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = System.Drawing.Color.Transparent
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

            _root.Controls.Add(dgv, 0, 0);
            _root.Controls.Add(bottom, 0, 1);
            Controls.Add(_root);

            Load += (s, e) => Cargar();
        

            UiStyle.Apply(this);
            UiFields.Apply(this);
            this.CancelButton = btnCerrar;

            UiHints.Attach(this, new (Control control, string hint)[]
            {
                (btnAgregar, "Ins"),
                (btnEditar, "Enter"),
                (btnEliminar, "Del"),
                (btnCerrar, "Esc"),
            });

            // Atajos reales (para que el hint no sea decorativo)
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

        private AuxRepo.MeseroDto? Seleccionado()
            => dgv.CurrentRow?.DataBoundItem as AuxRepo.MeseroDto;

        private void Cargar()
        {
            try
            {
                _meseros.Clear();
                var lista = AuxRepo.ListarMeseros(soloActivos: false);
                if (lista == null) return;

                foreach (var m in lista)
                    _meseros.Add(m);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudieron cargar los meseros.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Agregar()
        {
            if (!EditarMeseroDialog.Show(this, out string nombre, out bool activo))
                return;

            nombre = (nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show(this,
                    "El nombre del mesero no puede estar vacío.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                AuxRepo.InsertMesero(nombre, activo);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo agregar al mesero.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Editar()
        {
            var m = Seleccionado();
            if (m == null) return;

            if (!EditarMeseroDialog.Show(this, out string nombre, out bool activo, m.Nombre, m.Activo))
                return;

            nombre = (nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show(this,
                    "El nombre del mesero no puede estar vacío.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                AuxRepo.UpdateMesero(m.Id, nombre, activo);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo actualizar al mesero.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {

        }

        private void Eliminar()
        {
            var m = Seleccionado();
            if (m == null) return;

            if (MessageBox.Show(this,
                    $"¿Eliminar al mesero '{m.Nombre}'?",
                    "Confirmar",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                AuxRepo.DeleteMesero(m.Id);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo eliminar al mesero.\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private class EditarMeseroDialog : Form
        {
            private readonly TextBox txtNombre;
            private readonly CheckBox chkActivo;

            private EditarMeseroDialog(string? nombre, bool activo)
            {
                Text = "Mesero";
                StartPosition = FormStartPosition.CenterParent;
                Width = 360;
                Height = 160;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                var lbl1 = new Label { Text = "Nombre:", Left = 15, Top = 20, Width = 80 };
                txtNombre = new TextBox { Left = 100, Top = 16, Width = 220, Text = nombre ?? string.Empty };

                chkActivo = new CheckBox
                {
                    Left = 100,
                    Top = 50,
                    Width = 100,
                    Text = "Activo",
                    Checked = activo
                };

                var btnOk = new Button { Text = "OK", Left = 160, Top = 80, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancelar", Left = 245, Top = 80, Width = 75, DialogResult = DialogResult.Cancel };

                Controls.AddRange(new Control[] { lbl1, txtNombre, chkActivo, btnOk, btnCancel });
                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }

            public static bool Show(IWin32Window owner, out string nombre, out bool activo,
                string? nombreIni = null, bool activoIni = true)
            {
                using var f = new EditarMeseroDialog(nombreIni, activoIni);
                if (f.ShowDialog(owner) == DialogResult.OK)
                {
                    nombre = f.txtNombre.Text?.Trim() ?? string.Empty;
                    activo = f.chkActivo.Checked;
                    return true;
                }

                nombre = string.Empty;
                activo = true;
                return false;
            }
        }

}
}
