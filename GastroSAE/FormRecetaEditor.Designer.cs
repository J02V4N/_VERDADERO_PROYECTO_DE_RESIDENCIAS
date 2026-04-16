namespace GastroSAE
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
            cboPlatillo = new ComboBox();
            dgvIngredientes = new DataGridView();
            btnAgregarFila = new Button();
            btnGuardar = new Button();
            btnEliminarTodo = new Button();
            lblTotal = new Label();
            lblPlatillo = new Label();
            btnEliminarProducto = new Button();
            btnNuevoPlatillo = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvIngredientes).BeginInit();
            SuspendLayout();
            // 
            // cboPlatillo
            // 
            cboPlatillo.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPlatillo.Location = new Point(70, 12);
            cboPlatillo.Name = "cboPlatillo";
            cboPlatillo.Size = new Size(380, 33);
            cboPlatillo.TabIndex = 3;
            // 
            // dgvIngredientes
            // 
            dgvIngredientes.AllowUserToAddRows = false;
            dgvIngredientes.ColumnHeadersHeight = 34;
            dgvIngredientes.Location = new Point(12, 50);
            dgvIngredientes.Name = "dgvIngredientes";
            dgvIngredientes.RowHeadersVisible = false;
            dgvIngredientes.RowHeadersWidth = 62;
            dgvIngredientes.Size = new Size(880, 420);
            dgvIngredientes.TabIndex = 4;
            // 
            // btnAgregarFila
            // 
            btnAgregarFila.Location = new Point(320, 482);
            btnAgregarFila.Name = "btnAgregarFila";
            btnAgregarFila.Size = new Size(100, 27);
            btnAgregarFila.TabIndex = 7;
            btnAgregarFila.Text = "Agregar fila";
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(660, 482);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(100, 27);
            btnGuardar.TabIndex = 10;
            btnGuardar.Text = "Guardar";
            // 
            // btnEliminarTodo
            // 
            btnEliminarTodo.Location = new Point(540, 482);
            btnEliminarTodo.Name = "btnEliminarTodo";
            btnEliminarTodo.Size = new Size(110, 27);
            btnEliminarTodo.TabIndex = 9;
            btnEliminarTodo.Text = "Eliminar todo";
            // 
            // lblTotal
            // 
            lblTotal.AutoSize = true;
            lblTotal.Location = new Point(780, 488);
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new Size(205, 25);
            lblTotal.TabIndex = 11;
            lblTotal.Text = "Costo total receta: $0.00";
            // 
            // lblPlatillo
            // 
            lblPlatillo.AutoSize = true;
            lblPlatillo.Location = new Point(12, 15);
            lblPlatillo.Name = "lblPlatillo";
            lblPlatillo.Size = new Size(68, 25);
            lblPlatillo.TabIndex = 2;
            lblPlatillo.Text = "Platillo:";
            // 
            // btnEliminarProducto
            // 
            btnEliminarProducto.Location = new Point(540, 515);
            btnEliminarProducto.Name = "btnEliminarProducto";
            btnEliminarProducto.Size = new Size(110, 27);
            btnEliminarProducto.TabIndex = 0;
            btnEliminarProducto.Text = "Eliminar producto";
            btnEliminarProducto.UseVisualStyleBackColor = true;
            btnEliminarProducto.Click += btnEliminarProducto_Click;
            // 
            // btnNuevoPlatillo
            // 
            btnNuevoPlatillo.Location = new Point(12, 515);
            btnNuevoPlatillo.Name = "btnNuevoPlatillo";
            btnNuevoPlatillo.Size = new Size(120, 27);
            btnNuevoPlatillo.TabIndex = 1;
            btnNuevoPlatillo.Text = "Nuevo platillo";
            btnNuevoPlatillo.UseVisualStyleBackColor = true;
            btnNuevoPlatillo.Click += btnNuevoPlatillo_Click;
            // 
            // FormRecetaEditor
            // 
            ClientSize = new Size(898, 544);
            Controls.Add(btnEliminarProducto);
            Controls.Add(btnNuevoPlatillo);
            Controls.Add(lblPlatillo);
            Controls.Add(cboPlatillo);
            Controls.Add(dgvIngredientes);
            Controls.Add(btnAgregarFila);
            Controls.Add(btnEliminarTodo);
            Controls.Add(btnGuardar);
            Controls.Add(lblTotal);
            Name = "FormRecetaEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Editor de Receta";
            Load += FormRecetaEditor_Load;
            ((System.ComponentModel.ISupportInitialize)dgvIngredientes).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
