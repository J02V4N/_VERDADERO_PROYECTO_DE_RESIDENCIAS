```csharp
namespace PROYECTO_RESIDENCIAS
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabMain = new TabControl();
            tabMesas = new TabPage();
            statusMain = new StatusStrip();
            tslSae = new ToolStripStatusLabel();
            tslAux = new ToolStripStatusLabel();
            tslBascula = new ToolStripStatusLabel();
            panel1 = new Panel();
            btnLiberarMesa = new Button();
            btnAsignarMesero = new Button();
            btnAbrirMesa = new Button();
            lblMesaSel = new Label();
            cboMesero = new ComboBox();
            dgvMesas = new DataGridView();
            tabPedido = new TabPage();
            dgvReceta = new DataGridView();
            btnQuitarLinea = new Button();
            label1 = new Label();
            txtBuscarPlatillo = new TextBox();
            btnIrCobro = new Button();
            lblTotales = new Label();
            dgvPedido = new DataGridView();
            btnAgregarLinea = new Button();
            lbPlatillos = new ListBox();
            tabCobro = new TabPage();
            btnCobroCancelar = new Button();
            btnCobroConfirmar = new Button();
            lblCobroCambio = new Label();
            txtCobroRef = new TextBox();
            txtCobroTarjeta = new TextBox();
            txtCobroEfectivo = new TextBox();
            lblCobroTotal = new Label();
            lblCobroMesa = new Label();
            btnReimprimir = new Button();
            lblCambio = new Label();
            txtImporteRecibido = new TextBox();
            cboFormaPago = new ComboBox();
            cboMetodoPago = new ComboBox();
            lblResumenCobro = new Label();
            btnConfirmarCobro = new Button();
            cboUsoCFDI = new ComboBox();
            txtRazon = new TextBox();
            txtRFC = new TextBox();
            chkFacturarAhora = new CheckBox();
            tabInventario = new TabPage();
            btnInvLimpiar = new Button();
            btnInvEliminar = new Button();
            btnInvGuardarAux = new Button();
            lblInvTotales = new Label();
            dgvInvCaptura = new DataGridView();
            btnInvAgregar = new Button();
            txtInvCostoKg = new TextBox();
            lblInvKg = new Label();
            txtInvPesoGr = new TextBox();
            chkInvSimularBascula = new CheckBox();
            btnInvRefrescar = new Button();
            lbInvArticulos = new ListBox();
            txtInvBuscar = new TextBox();
            tabConfig = new TabPage();
            btnCfgMeseros = new Button();
            btnCfgMesas = new Button();
            btnGuardarConfig = new Button();
            btnProbarBascula = new Button();
            cboListaPrecios = new ComboBox();
            cboAlmacen = new ComboBox();
            txtPuertoCom = new TextBox();
            cboImpresora = new ComboBox();
            btnPruebaAux = new Button();
            btnPruebaSae = new Button();
            txtRutaAux = new TextBox();
            txtRutaSae = new TextBox();
            btnCfgRecetas = new Button();
            tabMain.SuspendLayout();
            tabMesas.SuspendLayout();
            statusMain.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMesas).BeginInit();
            tabPedido.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvReceta).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvPedido).BeginInit();
            tabCobro.SuspendLayout();
            tabInventario.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInvCaptura).BeginInit();
            tabConfig.SuspendLayout();
            SuspendLayout();
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabMesas);
            tabMain.Controls.Add(tabPedido);
            tabMain.Controls.Add(tabCobro);
            tabMain.Controls.Add(tabInventario);
            tabMain.Controls.Add(tabConfig);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 0);
            tabMain.Margin = new Padding(4, 5, 4, 5);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1899, 1023);
            tabMain.TabIndex = 0;
            // 
            // tabMesas
            // 
            tabMesas.Controls.Add(statusMain);
            tabMesas.Controls.Add(panel1);
            tabMesas.Controls.Add(dgvMesas);
            tabMesas.Location = new Point(4, 34);
            tabMesas.Margin = new Padding(4, 5, 4, 5);
            tabMesas.Name = "tabMesas";
            tabMesas.Padding = new Padding(4, 5, 4, 5);
            tabMesas.Size = new Size(1891, 985);
            tabMesas.TabIndex = 0;
            tabMesas.Text = "tabMesas";
            tabMesas.UseVisualStyleBackColor = true;
            // 
            // statusMain
            // 
            statusMain.ImageScalingSize = new Size(24, 24);
            statusMain.Items.AddRange(new ToolStripItem[] { tslSae, tslAux, tslBascula });
            statusMain.Location = new Point(547, 948);
            statusMain.Name = "statusMain";
            statusMain.Padding = new Padding(1, 0, 20, 0);
            statusMain.Size = new Size(1340, 32);
            statusMain.TabIndex = 4;
            statusMain.Text = "statusStrip1";
            // 
            // tslSae
            // 
            tslSae.Name = "tslSae";
            tslSae.Size = new Size(84, 25);
            tslSae.Text = "SAE: OFF";
            // 
            // tslAux
            // 
            tslAux.Name = "tslAux";
            tslAux.Size = new Size(83, 25);
            tslAux.Text = "Aux: OFF";
            // 
            // tslBascula
            // 
            tslBascula.Name = "tslBascula";
            tslBascula.Size = new Size(111, 25);
            tslBascula.Text = "Báscula: OFF";
            // 
            // panel1
            // 
            panel1.Controls.Add(btnLiberarMesa);
            panel1.Controls.Add(btnAsignarMesero);
            panel1.Controls.Add(btnAbrirMesa);
            panel1.Controls.Add(lblMesaSel);
            panel1.Controls.Add(cboMesero);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(547, 5);
            panel1.Margin = new Padding(4, 5, 4, 5);
            panel1.Name = "panel1";
            panel1.Size = new Size(1340, 975);
            panel1.TabIndex = 3;
            // 
            // btnLiberarMesa
            // 
            btnLiberarMesa.Location = new Point(240, 583);
            btnLiberarMesa.Margin = new Padding(4, 5, 4, 5);
            btnLiberarMesa.Name = "btnLiberarMesa";
            btnLiberarMesa.Size = new Size(107, 38);
            btnLiberarMesa.TabIndex = 6;
            btnLiberarMesa.Text = "btnLiberarMesa";
            btnLiberarMesa.UseVisualStyleBackColor = true;
            btnLiberarMesa.Click += btnLiberarMesa_Click;
            // 
            // btnAsignarMesero
            // 
            btnAsignarMesero.Location = new Point(419, 72);
            btnAsignarMesero.Margin = new Padding(4, 5, 4, 5);
            btnAsignarMesero.Name = "btnAsignarMesero";
            btnAsignarMesero.Size = new Size(107, 38);
            btnAsignarMesero.TabIndex = 5;
            btnAsignarMesero.Text = "Asignar";
            btnAsignarMesero.UseVisualStyleBackColor = true;
            btnAsignarMesero.Click += btnAsignarMesero_Click;
            // 
            // btnAbrirMesa
            // 
            btnAbrirMesa.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAbrirMesa.Location = new Point(970, 561);
            btnAbrirMesa.Margin = new Padding(4, 5, 4, 5);
            btnAbrirMesa.Name = "btnAbrirMesa";
            btnAbrirMesa.Size = new Size(173, 38);
            btnAbrirMesa.TabIndex = 1;
            btnAbrirMesa.Text = "Atender Mesa";
            btnAbrirMesa.UseVisualStyleBackColor = true;
            btnAbrirMesa.Click += btnAbrirMesa_Click;
            // 
            // lblMesaSel
            // 
            lblMesaSel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMesaSel.AutoSize = true;
            lblMesaSel.Location = new Point(1023, 42);
            lblMesaSel.Margin = new Padding(4, 0, 4, 0);
            lblMesaSel.Name = "lblMesaSel";
            lblMesaSel.Size = new Size(188, 25);
            lblMesaSel.TabIndex = 0;
            lblMesaSel.Text = "Sin mesa seleccionada";
            // 
            // cboMesero
            // 
            cboMesero.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cboMesero.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMesero.FormattingEnabled = true;
            cboMesero.Location = new Point(970, 72);
            cboMesero.Margin = new Padding(4, 5, 4, 5);
            cboMesero.Name = "cboMesero";
            cboMesero.Size = new Size(171, 33);
            cboMesero.TabIndex = 2;
            // 
            // dgvMesas
            // 
            dgvMesas.AllowUserToAddRows = false;
            dgvMesas.AllowUserToDeleteRows = false;
            dgvMesas.AllowUserToOrderColumns = true;
            dgvMesas.AllowUserToResizeColumns = false;
            dgvMesas.AllowUserToResizeRows = false;
            dgvMesas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMesas.Dock = DockStyle.Left;
            dgvMesas.Location = new Point(4, 5);
            dgvMesas.Margin = new Padding(4, 5, 4, 5);
            dgvMesas.MultiSelect = false;
            dgvMesas.Name = "dgvMesas";
            dgvMesas.ReadOnly = true;
            dgvMesas.RowHeadersVisible = false;
            dgvMesas.RowHeadersWidth = 62;
            dgvMesas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMesas.Size = new Size(543, 975);
            dgvMesas.TabIndex = 0;
            // 
            // tabPedido
            // 
            tabPedido.Controls.Add(dgvReceta);
            tabPedido.Controls.Add(btnQuitarLinea);
            tabPedido.Controls.Add(label1);
            tabPedido.Controls.Add(txtBuscarPlatillo);
            tabPedido.Controls.Add(btnIrCobro);
            tabPedido.Controls.Add(lblTotales);
            tabPedido.Controls.Add(dgvPedido);
            tabPedido.Controls.Add(btnAgregarLinea);
            tabPedido.Controls.Add(lbPlatillos);
            tabPedido.Location = new Point(4, 34);
            tabPedido.Margin = new Padding(4, 5, 4, 5);
            tabPedido.Name = "tabPedido";
            tabPedido.Padding = new Padding(4, 5, 4, 5);
            tabPedido.Size = new Size(1891, 985);
            tabPedido.TabIndex = 1;
            tabPedido.Text = "tabPedido";
            tabPedido.UseVisualStyleBackColor = true;
            // 
            // dgvReceta
            // 
            dgvReceta.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReceta.Location = new Point(1011, 613);
            dgvReceta.Name = "dgvReceta";
            dgvReceta.RowHeadersWidth = 62;
            dgvReceta.Size = new Size(871, 362);
            dgvReceta.TabIndex = 17;
            // 
            // btnQuitarLinea
            // 
            btnQuitarLinea.Location = new Point(587, 572);
            btnQuitarLinea.Name = "btnQuitarLinea";
            btnQuitarLinea.Size = new Size(169, 33);
            btnQuitarLinea.TabIndex = 16;
            btnQuitarLinea.Text = "Eliminar platillo";
            btnQuitarLinea.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(4, 5);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(75, 25);
            label1.TabIndex = 15;
            label1.Text = "Buscar...";
            // 
            // txtBuscarPlatillo
            // 
            txtBuscarPlatillo.Location = new Point(4, 30);
            txtBuscarPlatillo.Margin = new Padding(4, 5, 4, 5);
            txtBuscarPlatillo.Name = "txtBuscarPlatillo";
            txtBuscarPlatillo.Size = new Size(398, 31);
            txtBuscarPlatillo.TabIndex = 14;
            txtBuscarPlatillo.TextChanged += txtBuscarPlatillo_TextChanged;
            // 
            // btnIrCobro
            // 
            btnIrCobro.Location = new Point(1564, 127);
            btnIrCobro.Margin = new Padding(4, 5, 4, 5);
            btnIrCobro.Name = "btnIrCobro";
            btnIrCobro.Size = new Size(107, 38);
            btnIrCobro.TabIndex = 13;
            btnIrCobro.Text = "Cobrar";
            btnIrCobro.UseVisualStyleBackColor = true;
            btnIrCobro.Click += btnIrCobro_Click;
            // [...] (designer content continues)
        }

        #endregion

        private TabControl tabMain;
        private TabPage tabMesas;
        private TabPage tabPedido;
        private TabPage tabCobro;
        private TabPage tabConfig;
        private DataGridView dgvMesas;
        private ComboBox cboMesero;
        private Button btnAbrirMesa;
        private Label lblMesaSel;
        private Panel panel1;
        private ComboBox cboUsoCFDI;
        private TextBox txtRazon;
        private TextBox txtRFC;
        private CheckBox chkFacturarAhora;
        private Label lblResumenCobro;
        private Button btnConfirmarCobro;
        private ListBox lbPlatillos;
        private Button btnIrCobro;
        private Label lblTotales;
        private DataGridView dgvPedido;
        private Button btnAgregarLinea;
        private Button btnAsignarMesero;
        private StatusStrip statusMain;
        private TextBox txtBuscarPlatillo;
        private ComboBox cboFormaPago;
        private ComboBox cboMetodoPago;
        private TextBox txtImporteRecibido;
        private Button btnReimprimir;
        private Label lblCambio;
        private TextBox txtRutaSae;
        private TextBox txtPuertoCom;
        private ComboBox cboImpresora;
        private Button btnPruebaAux;
        private Button btnPruebaSae;
        private TextBox txtRutaAux;
        private ComboBox cboListaPrecios;
        private ComboBox cboAlmacen;
        private Button btnProbarBascula;
        private TabPage tabInventario;
        private ListBox lbInvArticulos;
        private TextBox txtInvBuscar;
        private Button btnInvAgregar;
        private TextBox txtInvCostoKg;
        private Label lblInvKg;
        private TextBox txtInvPesoGr;
        private CheckBox chkInvSimularBascula;
        private Button btnInvRefrescar;
        private DataGridView dgvInvCaptura;
        private Label lblInvTotales;
        private Button btnInvGuardarAux;
        private Button btnInvLimpiar;
        private Button btnInvEliminar;
        private Button btnGuardarConfig;
        private ToolStripStatusLabel tslSae;
        private ToolStripStatusLabel tslAux;
        private ToolStripStatusLabel tslBascula;
        private Button btnCfgMesas;
        private Button btnCfgMeseros;
        private Button btnLiberarMesa;
        private Label label1;
        private Button btnQuitarLinea;
        private DataGridView dgvReceta;
        private Button btnCobroConfirmar;
        private Label lblCobroCambio;
        private TextBox txtCobroRef;
        private TextBox txtCobroTarjeta;
        private TextBox txtCobroEfectivo;
        private Label lblCobroTotal;
        private Label lblCobroMesa;
        private Button btnCobroCancelar;
        private Button btnCfgRecetas;
    }
}
```