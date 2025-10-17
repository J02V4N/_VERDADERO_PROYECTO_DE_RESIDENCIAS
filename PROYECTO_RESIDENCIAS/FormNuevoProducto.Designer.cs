using PROYECTO_RESIDENCIAS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROYECTO_RESIDENCIAS
{
    partial class FormNuevoProducto
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtClave;
        private TextBox txtDescripcion;
        private TextBox txtUnidad;
        private Button btnOk;
        private Button btnCancel;
        private Label lblClave;
        private Label lblDesc;
        private Label lblUM;
        private System.Windows.Forms.Label lblPrefijo;


        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            // Instanciación (arriba, antes de tocar propiedades)
            lblPrefijo = new Label();
            txtClave = new TextBox();
            txtDescripcion = new TextBox();
            txtUnidad = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();

            // ... ahora sí, setear propiedades:
            lblPrefijo.AutoSize = true;
            lblPrefijo.Location = new Point(90, 15);
            lblPrefijo.Name = "lblPrefijo";
            lblPrefijo.Text = "Prep";

            txtClave.Location = new Point(130, 12);  // ya no será null porque lo instanciaste arriba
            txtClave.Size = new Size(260, 23);
            txtClave.MaxLength = 12;

            




            components = new System.ComponentModel.Container();
            txtClave = new TextBox();
            txtDescripcion = new TextBox();
            txtUnidad = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            lblClave = new Label();
            lblDesc = new Label();
            lblUM = new Label();


            this.Text = "Nuevo producto (SAE)";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(420, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;




            lblClave.AutoSize = true;
            lblClave.Location = new Point(12, 15);
            lblClave.Text = "Clave:";

            txtClave.Location = new Point(90, 12);
            txtClave.Size = new Size(300, 23);

            lblDesc.AutoSize = true;
            lblDesc.Location = new Point(12, 50);
            lblDesc.Text = "Descripción:";

            txtDescripcion.Location = new Point(90, 47);
            txtDescripcion.Size = new Size(300, 23);

            lblUM.AutoSize = true;
            lblUM.Location = new Point(12, 85);
            lblUM.Text = "Unidad:";

            txtUnidad.Location = new Point(90, 82);
            txtUnidad.Size = new Size(120, 23);
            txtUnidad.Text = "PZA";

            btnOk.Location = new Point(210, 120);
            btnOk.Size = new Size(85, 27);
            btnOk.Text = "Crear";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtClave.Text) || string.IsNullOrWhiteSpace(txtDescripcion.Text))
                {
                    MessageBox.Show("Clave y Descripción son obligatorios.");
                    this.DialogResult = DialogResult.None;
                }
            };

            btnCancel.Location = new Point(305, 120);
            btnCancel.Size = new Size(85, 27);
            btnCancel.Text = "Cancelar";
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.Add(lblClave);
            Controls.Add(txtClave);
            Controls.Add(lblDesc);
            Controls.Add(txtDescripcion);
            Controls.Add(lblUM);
            Controls.Add(txtUnidad);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            // ... resto de propiedades y finalmente:
            this.Controls.Add(lblPrefijo);
            this.Controls.Add(txtClave);
            // etc.
        }
    }
}
