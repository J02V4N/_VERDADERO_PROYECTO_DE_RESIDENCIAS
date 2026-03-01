```csharp
namespace PROYECTO_RESIDENCIAS
{
    partial class FormNuevoProducto
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TextBox txtClave;
        private System.Windows.Forms.TextBox txtDescripcion;
        private System.Windows.Forms.TextBox txtUnidad;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblClave;
        private System.Windows.Forms.Label lblDesc;
        private System.Windows.Forms.Label lblUM;
        private System.Windows.Forms.Label lblPrefijo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.txtClave = new System.Windows.Forms.TextBox();
            this.txtDescripcion = new System.Windows.Forms.TextBox();
            this.txtUnidad = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblClave = new System.Windows.Forms.Label();
            this.lblDesc = new System.Windows.Forms.Label();
            this.lblUM = new System.Windows.Forms.Label();
            this.lblPrefijo = new System.Windows.Forms.Label();

            this.SuspendLayout();

            // Form
            this.Text = "Nuevo producto (SAE)";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(420, 200);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // lblClave
            this.lblClave.AutoSize = true;
            this.lblClave.Location = new System.Drawing.Point(12, 15);
            this.lblClave.Text = "Clave:";

            // lblPrefijo
            this.lblPrefijo.AutoSize = true;
            this.lblPrefijo.Location = new System.Drawing.Point(90, 15);
            this.lblPrefijo.Name = "lblPrefijo";
            this.lblPrefijo.Text = "Prep";

            // txtClave
            this.txtClave.Location = new System.Drawing.Point(130, 12);
            this.txtClave.Size = new System.Drawing.Size(260, 23);
            this.txtClave.MaxLength = 12;

            // lblDesc
            this.lblDesc.AutoSize = true;
            this.lblDesc.Location = new System.Drawing.Point(12, 50);
            this.lblDesc.Text = "Descripción:";

            // txtDescripcion
            this.txtDescripcion.Location = new System.Drawing.Point(90, 47);
            this.txtDescripcion.Size = new System.Drawing.Size(300, 23);

            // lblUM
            this.lblUM.AutoSize = true;
            this.lblUM.Location = new System.Drawing.Point(12, 85);
            this.lblUM.Text = "Unidad:";

            // txtUnidad
            this.txtUnidad.Location = new System.Drawing.Point(90, 82);
            this.txtUnidad.Size = new System.Drawing.Size(120, 23);
            this.txtUnidad.Text = "PZA";

            // btnOk
            this.btnOk.Location = new System.Drawing.Point(210, 120);
            this.btnOk.Size = new System.Drawing.Size(85, 27);
            this.btnOk.Text = "Crear";
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(305, 120);
            this.btnCancel.Size = new System.Drawing.Size(85, 27);
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // Controls
            this.Controls.Add(this.lblClave);
            this.Controls.Add(this.lblPrefijo);
            this.Controls.Add(this.txtClave);
            this.Controls.Add(this.lblDesc);
            this.Controls.Add(this.txtDescripcion);
            this.Controls.Add(this.lblUM);
            this.Controls.Add(this.txtUnidad);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);

            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
```