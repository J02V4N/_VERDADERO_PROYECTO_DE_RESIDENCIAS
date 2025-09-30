using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    public class FormMesasConfig : Form
    {
        private DataGridView dgv;
        private Button btnAgregar, btnEditar, btnEliminar, btnCerrar;

        public FormMesasConfig()
        {
            Text = "Mesas (BD Aux)";
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
                AutoGenerateColumns = false
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id", DataPropertyName = "Id", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Nombre", Width = 250 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cap.", DataPropertyName = "Capacidad", Width = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Estado", Width = 90 });

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

        private AuxRepo.MesaDto? Seleccionada()
            => dgv.CurrentRow?.DataBoundItem as AuxRepo.MesaDto;

        private void Cargar() => dgv.DataSource = new BindingList<AuxRepo.MesaDto>(AuxRepo.ListarMesas());

        private void Agregar()
        {
            if (EditarMesaDialog.Show(this, out string nombre, out int? cap))
            {
                AuxRepo.InsertMesa(nombre, cap);
                Cargar();
            }
        }

        private void Editar()
        {
            var m = Seleccionada(); if (m == null) return;
            if (EditarMesaDialog.Show(this, out string nombre, out int? cap, m.Nombre, m.Capacidad))
            {
                AuxRepo.UpdateMesa(m.Id, nombre, cap);
                Cargar();
            }
        }

        private void Eliminar()
        {
            var m = Seleccionada(); if (m == null) return;
            if (MessageBox.Show(this, $"¿Eliminar la mesa '{m.Nombre}'?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                AuxRepo.DeleteMesa(m.Id);
                Cargar();
            }
        }

        // Dialogo simple para captura/edición
        private class EditarMesaDialog : Form
        {
            private TextBox txtNombre; private NumericUpDown numCap; private Button btnOk, btnCancel;

            private EditarMesaDialog(string nombre, int? cap)
            {
                Text = "Mesa"; StartPosition = FormStartPosition.CenterParent;
                Width = 360; Height = 180; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;

                var lbl1 = new Label { Text = "Nombre:", Left = 15, Top = 20, Width = 80 };
                txtNombre = new TextBox { Left = 100, Top = 16, Width = 220, Text = nombre ?? "" };

                var lbl2 = new Label { Text = "Capacidad:", Left = 15, Top = 55, Width = 80 };
                numCap = new NumericUpDown { Left = 100, Top = 52, Width = 80, Minimum = 0, Maximum = 50, Value = (decimal)(cap ?? 0) };

                btnOk = new Button { Text = "OK", Left = 160, Top = 90, Width = 75, DialogResult = DialogResult.OK };
                btnCancel = new Button { Text = "Cancelar", Left = 245, Top = 90, Width = 75, DialogResult = DialogResult.Cancel };

                Controls.AddRange(new Control[] { lbl1, txtNombre, lbl2, numCap, btnOk, btnCancel });
                AcceptButton = btnOk; CancelButton = btnCancel;
            }

            public static bool Show(IWin32Window owner, out string nombre, out int? cap, string nombreIni = null, int? capIni = null)
            {
                using var f = new EditarMesaDialog(nombreIni, capIni);
                if (f.ShowDialog(owner) == DialogResult.OK)
                {
                    nombre = f.txtNombre.Text?.Trim() ?? "";
                    cap = (int)f.numCap.Value;
                    return true;
                }
                nombre = ""; cap = null; return false;
            }
        }
    }
}
