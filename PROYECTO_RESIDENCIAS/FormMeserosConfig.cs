using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    public class FormMeserosConfig : Form
    {
        private DataGridView dgv;
        private Button btnAgregar, btnEditar, btnEliminar, btnCerrar;

        public FormMeserosConfig()
        {
            Text = "Meseros (BD Aux)";
            StartPosition = FormStartPosition.CenterParent;
            Width = 520; Height = 420;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            dgv = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 300,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            }; 
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id", DataPropertyName = "Id", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Nombre", Width = 260 });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Activo", DataPropertyName = "Activo", Width = 80 });

            btnAgregar = new Button { Text = "Agregar", Left = 20, Top = 320, Width = 90 };
            btnEditar = new Button { Text = "Editar", Left = 120, Top = 320, Width = 90 };
            btnEliminar = new Button { Text = "Eliminar", Left = 220, Top = 320, Width = 90 };
            btnCerrar = new Button { Text = "Cerrar", Left = 400, Top = 320, Width = 90 };

            btnAgregar.Click += (s, e) => Agregar();
            btnEditar.Click += (s, e) => Editar();
            btnEliminar.Click += (s, e) => Eliminar();
            btnCerrar.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { dgv, btnAgregar, btnEditar, btnEliminar, btnCerrar });

            Cargar();
        }

        private AuxRepo.MeseroDto? Seleccionado()
            => dgv.CurrentRow?.DataBoundItem as AuxRepo.MeseroDto;

        private void Cargar()
        {
            dgv.DataSource = new BindingList<AuxRepo.MeseroDto>(
            AuxRepo.ListarMeseros(soloActivos: false));
        }

        private void Agregar()
        {
            if (EditarMeseroDialog.Show(this, out string nombre, out bool activo))
            {
                AuxRepo.InsertMesero(nombre, activo);
                Cargar();
            }
        }

        private void Editar()
        {
            var m = Seleccionado(); if (m == null) return;
            if (EditarMeseroDialog.Show(this, out string nombre, out bool activo, m.Nombre, m.Activo))
            {
                AuxRepo.UpdateMesero(m.Id, nombre, activo);
                Cargar();
            }
        }

        private void Eliminar()
        {
            var m = Seleccionado(); if (m == null) return;
            if (MessageBox.Show(this, $"¿Eliminar al mesero '{m.Nombre}'?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                AuxRepo.DeleteMesero(m.Id);
                Cargar();
            }
        }

        private class EditarMeseroDialog : Form
        {
            private TextBox txtNombre; private CheckBox chkActivo; private Button btnOk, btnCancel;

            private EditarMeseroDialog(string nombre, bool activo)
            {
                Text = "Mesero"; StartPosition = FormStartPosition.CenterParent;
                Width = 360; Height = 160; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;

                var lbl1 = new Label { Text = "Nombre:", Left = 15, Top = 20, Width = 80 };
                txtNombre = new TextBox { Left = 100, Top = 16, Width = 220, Text = nombre ?? "" };

                chkActivo = new CheckBox { Left = 100, Top = 50, Width = 100, Text = "Activo", Checked = activo };

                btnOk = new Button { Text = "OK", Left = 160, Top = 80, Width = 75, DialogResult = DialogResult.OK };
                btnCancel = new Button { Text = "Cancelar", Left = 245, Top = 80, Width = 75, DialogResult = DialogResult.Cancel };

                Controls.AddRange(new Control[] { lbl1, txtNombre, chkActivo, btnOk, btnCancel });
                AcceptButton = btnOk; CancelButton = btnCancel;
            }

            public static bool Show(IWin32Window owner, out string nombre, out bool activo, string nombreIni = null, bool activoIni = true)
            {
                using var f = new EditarMeseroDialog(nombreIni, activoIni);
                if (f.ShowDialog(owner) == DialogResult.OK)
                {
                    nombre = f.txtNombre.Text?.Trim() ?? "";
                    activo = f.chkActivo.Checked;
                    return true;
                }
                nombre = ""; activo = true; return false;
            }
        }
    }
}
