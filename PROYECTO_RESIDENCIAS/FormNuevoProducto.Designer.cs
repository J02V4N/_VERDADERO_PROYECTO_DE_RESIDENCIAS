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

        private System.Windows.Forms.TableLayoutPanel tlMain;
        private System.Windows.Forms.Panel pnlClave;
        private System.Windows.Forms.FlowLayoutPanel flButtons;

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

            this.tlMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlClave = new System.Windows.Forms.Panel();
            this.flButtons = new System.Windows.Forms.FlowLayoutPanel();

            this.SuspendLayout();

            // Form
            this.Text = "Nuevo producto (SAE)";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(520, 240);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // tlMain
            this.tlMain.ColumnCount = 2;
            this.tlMain.RowCount = 4;
            this.tlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlMain.Padding = new System.Windows.Forms.Padding(18, 16, 18, 16);
            this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // lblClave
            this.lblClave.AutoSize = true;
            this.lblClave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top)));
            this.lblClave.Margin = new System.Windows.Forms.Padding(0, 10, 10, 0);
            this.lblClave.Text = "Clave:";

            // lblPrefijo
            this.lblPrefijo.AutoSize = true;
            this.lblPrefijo.Location = new System.Drawing.Point(0, 10);
            this.lblPrefijo.Name = "lblPrefijo";
            this.lblPrefijo.Text = "Prep";

            // txtClave
            this.txtClave.Location = new System.Drawing.Point(44, 6);
            this.txtClave.Size = new System.Drawing.Size(360, 23);
            this.txtClave.MaxLength = 12;

            // lblDesc
            this.lblDesc.AutoSize = true;
            this.lblDesc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top)));
            this.lblDesc.Margin = new System.Windows.Forms.Padding(0, 10, 10, 0);
            this.lblDesc.Text = "Descripción:";

            // txtDescripcion
            this.txtDescripcion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDescripcion.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.txtDescripcion.Size = new System.Drawing.Size(360, 23);

            // lblUM
            this.lblUM.AutoSize = true;
            this.lblUM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top)));
            this.lblUM.Margin = new System.Windows.Forms.Padding(0, 10, 10, 0);
            this.lblUM.Text = "Unidad:";

            // txtUnidad
            this.txtUnidad.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.txtUnidad.Size = new System.Drawing.Size(120, 23);
            this.txtUnidad.Text = "pz";

            // btnOk
            this.btnOk.Size = new System.Drawing.Size(110, 36);
            this.btnOk.Text = "Crear";
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;

            // btnCancel
            this.btnCancel.Size = new System.Drawing.Size(110, 36);
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // pnlClave
            this.pnlClave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlClave.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.pnlClave.Controls.Add(this.lblPrefijo);
            this.pnlClave.Controls.Add(this.txtClave);

            // flButtons
            this.flButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flButtons.WrapContents = false;
            this.flButtons.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.flButtons.Controls.Add(this.btnCancel);
            this.flButtons.Controls.Add(this.btnOk);

            // tlMain children
            this.tlMain.Controls.Add(this.lblClave, 0, 0);
            this.tlMain.Controls.Add(this.pnlClave, 1, 0);
            this.tlMain.Controls.Add(this.lblDesc, 0, 1);
            this.tlMain.Controls.Add(this.txtDescripcion, 1, 1);
            this.tlMain.Controls.Add(this.lblUM, 0, 2);
            this.tlMain.Controls.Add(this.txtUnidad, 1, 2);
            this.tlMain.Controls.Add(this.flButtons, 1, 3);

            // Controls
            this.Controls.Add(this.tlMain);

            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
