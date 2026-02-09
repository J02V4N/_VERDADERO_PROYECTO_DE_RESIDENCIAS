using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    public class FormMeserosConfig : Form
    {
        private readonly DataGridView dgv;
        private readonly Button btnAgregar, btnEditar, btnEliminar, btnCerrar;

        private readonly BindingList<AuxRepo.MeseroDto> _meseros = new BindingList<AuxRepo.MeseroDto>();

        public FormMeserosConfig()
        {
            Text = "Meseros (BD Aux)";
            StartPosition = FormStartPosition.CenterParent;
            Width = 520;
            Height = 420;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            dgv = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
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

            btnAgregar = new Button { Text = "Agregar", Left = 20, Top = 320, Width = 90 };
            btnEditar = new Button { Text = "Editar", Left = 120, Top = 320, Width = 90 };
            btnEliminar = new Button { Text = "Eliminar", Left = 220, Top = 320, Width = 90 };
            btnCerrar = new Button { Text = "Cerrar", Left = 400, Top = 320, Width = 90 };

            btnAgregar.Click += (s, e) => Agregar();
            btnEditar.Click += (s, e) => Editar();
            btnEliminar.Click += (s, e) => Eliminar();
            btnCerrar.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { dgv, btnAgregar, btnEditar, btnEliminar, btnCerrar });

            Load += (s, e) => Cargar();
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
