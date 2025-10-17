namespace PROYECTO_RESIDENCIAS
{
    partial class FormRecetaEditor
    {
        private System.ComponentModel.IContainer components = null;
        private ComboBox cboPlatillo;
        private DataGridView dgvIngredientes;
        private Button btnAgregarFila;
        private Button btnGuardar;
        private Button btnEliminarTodo;
        private Button btnEliminarProducto;
        private Button btnReenumerar;
        private CheckBox chkAutoEnumerar;
        private NumericUpDown nudLinea;
        private Label lblTotal;
        private Label lblPlatillo;
        private Button btnNuevoPlatillo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            cboPlatillo = new ComboBox();
            dgvIngredientes = new DataGridView();
            btnAgregarFila = new Button();
            btnGuardar = new Button();
            btnEliminarTodo = new Button();
            btnReenumerar = new Button();
            chkAutoEnumerar = new CheckBox();
            nudLinea = new NumericUpDown();
            lblTotal = new Label();
            lblPlatillo = new Label();

            SuspendLayout();

            // Form
            this.Text = "Editor de Receta";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(920, 600);

            // lblPlatillo
            lblPlatillo.AutoSize = true;
            lblPlatillo.Location = new Point(12, 15);
            lblPlatillo.Text = "Platillo:";

            // cboPlatillo
            cboPlatillo.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPlatillo.Location = new Point(70, 12);
            cboPlatillo.Size = new Size(380, 23);

            // dgvIngredientes
            dgvIngredientes.Location = new Point(12, 50);
            dgvIngredientes.Size = new Size(880, 420);
            dgvIngredientes.AllowUserToAddRows = false;
            dgvIngredientes.AllowUserToDeleteRows = true;
            dgvIngredientes.RowHeadersVisible = false;

            // nudLinea
            nudLinea.Location = new Point(12, 485);
            nudLinea.Minimum = 1;
            nudLinea.Maximum = 100000;
            nudLinea.Value = 10;

            // chkAutoEnumerar
            chkAutoEnumerar.AutoSize = true;
            chkAutoEnumerar.Location = new Point(120, 486);
            chkAutoEnumerar.Text = "Auto enumerar (10,20,30…)";

            // btnAgregarFila
            btnAgregarFila.Location = new Point(320, 482);
            btnAgregarFila.Size = new Size(100, 27);
            btnAgregarFila.Text = "Agregar fila";

            // btnReenumerar
            btnReenumerar.Location = new Point(430, 482);
            btnReenumerar.Size = new Size(100, 27);
            btnReenumerar.Text = "Reenumerar";

            // btnEliminarTodo
            btnEliminarTodo.Location = new Point(540, 482);
            btnEliminarTodo.Size = new Size(110, 27);
            btnEliminarTodo.Text = "Eliminar todo";

            // btnEliminarProducto
            btnEliminarProducto = new Button();
            btnEliminarProducto.Location = new Point(540, 515);   // ajusta posición a tu layout
            btnEliminarProducto.Size = new Size(110, 27);
            btnEliminarProducto.Text = "Eliminar producto";
            btnEliminarProducto.UseVisualStyleBackColor = true;
            btnEliminarProducto.Click += btnEliminarProducto_Click;

            Controls.Add(btnEliminarProducto);

            // btnNuevoPlatillo
            btnNuevoPlatillo = new Button();
            btnNuevoPlatillo.Location = new Point(12, 515);   // ajusta al layout
            btnNuevoPlatillo.Size = new Size(120, 27);
            btnNuevoPlatillo.Text = "Nuevo platillo";
            btnNuevoPlatillo.UseVisualStyleBackColor = true;
            btnNuevoPlatillo.Click += btnNuevoPlatillo_Click;

            Controls.Add(btnNuevoPlatillo);



            // btnGuardar
            btnGuardar.Location = new Point(660, 482);
            btnGuardar.Size = new Size(100, 27);
            btnGuardar.Text = "Guardar";

            // lblTotal
            lblTotal.AutoSize = true;
            lblTotal.Location = new Point(780, 488);
            lblTotal.Text = "Costo total receta: $0.00";

            // Add controls
            Controls.Add(lblPlatillo);
            Controls.Add(cboPlatillo);
            Controls.Add(dgvIngredientes);
            Controls.Add(nudLinea);
            Controls.Add(chkAutoEnumerar);
            Controls.Add(btnAgregarFila);
            Controls.Add(btnReenumerar);
            Controls.Add(btnEliminarTodo);
            Controls.Add(btnGuardar);
            Controls.Add(lblTotal);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
