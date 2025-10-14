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
            label1 = new Label();
            txtBuscarPlatillo = new TextBox();
            btnIrCobro = new Button();
            lblTotales = new Label();
            dgvPedido = new DataGridView();
            btnAgregarLinea = new Button();
            txtPesoGr = new TextBox();
            chkSimularBascula = new CheckBox();
            lbPlatillos = new ListBox();
            tabCobro = new TabPage();
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
            btnQuitarLinea = new Button();
            tabMain.SuspendLayout();
            tabMesas.SuspendLayout();
            statusMain.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMesas).BeginInit();
            tabPedido.SuspendLayout();
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
            tabMain.Size = new Size(1143, 750);
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
            tabMesas.Size = new Size(1135, 712);
            tabMesas.TabIndex = 0;
            tabMesas.Text = "tabMesas";
            tabMesas.UseVisualStyleBackColor = true;
            // 
            // statusMain
            // 
            statusMain.ImageScalingSize = new Size(24, 24);
            statusMain.Items.AddRange(new ToolStripItem[] { tslSae, tslAux, tslBascula });
            statusMain.Location = new Point(547, 675);
            statusMain.Name = "statusMain";
            statusMain.Padding = new Padding(1, 0, 20, 0);
            statusMain.Size = new Size(584, 32);
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
            panel1.Size = new Size(584, 702);
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
            btnAbrirMesa.Location = new Point(213, 289);
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
            lblMesaSel.Location = new Point(267, 42);
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
            cboMesero.Location = new Point(213, 72);
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
            dgvMesas.Size = new Size(543, 702);
            dgvMesas.TabIndex = 0;
            // 
            // tabPedido
            // 
            tabPedido.Controls.Add(btnQuitarLinea);
            tabPedido.Controls.Add(label1);
            tabPedido.Controls.Add(txtBuscarPlatillo);
            tabPedido.Controls.Add(btnIrCobro);
            tabPedido.Controls.Add(lblTotales);
            tabPedido.Controls.Add(dgvPedido);
            tabPedido.Controls.Add(btnAgregarLinea);
            tabPedido.Controls.Add(txtPesoGr);
            tabPedido.Controls.Add(chkSimularBascula);
            tabPedido.Controls.Add(lbPlatillos);
            tabPedido.Location = new Point(4, 34);
            tabPedido.Margin = new Padding(4, 5, 4, 5);
            tabPedido.Name = "tabPedido";
            tabPedido.Padding = new Padding(4, 5, 4, 5);
            tabPedido.Size = new Size(1135, 712);
            tabPedido.TabIndex = 1;
            tabPedido.Text = "tabPedido";
            tabPedido.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(429, 12);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(75, 25);
            label1.TabIndex = 15;
            label1.Text = "Buscar...";
            // 
            // txtBuscarPlatillo
            // 
            txtBuscarPlatillo.Location = new Point(429, 42);
            txtBuscarPlatillo.Margin = new Padding(4, 5, 4, 5);
            txtBuscarPlatillo.Name = "txtBuscarPlatillo";
            txtBuscarPlatillo.Size = new Size(370, 31);
            txtBuscarPlatillo.TabIndex = 14;
            txtBuscarPlatillo.TextChanged += txtBuscarPlatillo_TextChanged;
            // 
            // btnIrCobro
            // 
            btnIrCobro.Location = new Point(960, 163);
            btnIrCobro.Margin = new Padding(4, 5, 4, 5);
            btnIrCobro.Name = "btnIrCobro";
            btnIrCobro.Size = new Size(107, 38);
            btnIrCobro.TabIndex = 13;
            btnIrCobro.Text = "Cobrar";
            btnIrCobro.UseVisualStyleBackColor = true;
            btnIrCobro.Click += btnIrCobro_Click;
            // 
            // lblTotales
            // 
            lblTotales.AutoSize = true;
            lblTotales.Location = new Point(989, 102);
            lblTotales.Margin = new Padding(4, 0, 4, 0);
            lblTotales.Name = "lblTotales";
            lblTotales.Size = new Size(59, 25);
            lblTotales.TabIndex = 12;
            lblTotales.Text = "label1";
            // 
            // dgvPedido
            // 
            dgvPedido.AllowUserToAddRows = false;
            dgvPedido.AllowUserToDeleteRows = false;
            dgvPedido.AllowUserToResizeColumns = false;
            dgvPedido.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPedido.Dock = DockStyle.Bottom;
            dgvPedido.Location = new Point(402, 340);
            dgvPedido.Margin = new Padding(4, 5, 4, 5);
            dgvPedido.Name = "dgvPedido";
            dgvPedido.RowHeadersVisible = false;
            dgvPedido.RowHeadersWidth = 62;
            dgvPedido.Size = new Size(729, 367);
            dgvPedido.TabIndex = 11;
            // 
            // btnAgregarLinea
            // 
            btnAgregarLinea.Location = new Point(494, 187);
            btnAgregarLinea.Margin = new Padding(4, 5, 4, 5);
            btnAgregarLinea.Name = "btnAgregarLinea";
            btnAgregarLinea.Size = new Size(156, 38);
            btnAgregarLinea.TabIndex = 10;
            btnAgregarLinea.Text = "Agregar";
            btnAgregarLinea.UseVisualStyleBackColor = true;
            btnAgregarLinea.Click += btnAgregarLinea_Click;
            // 
            // txtPesoGr
            // 
            txtPesoGr.Location = new Point(494, 97);
            txtPesoGr.Margin = new Padding(4, 5, 4, 5);
            txtPesoGr.Name = "txtPesoGr";
            txtPesoGr.ReadOnly = true;
            txtPesoGr.Size = new Size(154, 31);
            txtPesoGr.TabIndex = 9;
            txtPesoGr.TextAlign = HorizontalAlignment.Right;
            // 
            // chkSimularBascula
            // 
            chkSimularBascula.AutoSize = true;
            chkSimularBascula.Location = new Point(494, 145);
            chkSimularBascula.Margin = new Padding(4, 5, 4, 5);
            chkSimularBascula.Name = "chkSimularBascula";
            chkSimularBascula.Size = new Size(160, 29);
            chkSimularBascula.TabIndex = 8;
            chkSimularBascula.Text = "Simular Bascula";
            chkSimularBascula.UseVisualStyleBackColor = true;
            // 
            // lbPlatillos
            // 
            lbPlatillos.Dock = DockStyle.Left;
            lbPlatillos.FormattingEnabled = true;
            lbPlatillos.IntegralHeight = false;
            lbPlatillos.ItemHeight = 25;
            lbPlatillos.Location = new Point(4, 5);
            lbPlatillos.Margin = new Padding(4, 5, 4, 5);
            lbPlatillos.Name = "lbPlatillos";
            lbPlatillos.Size = new Size(398, 702);
            lbPlatillos.TabIndex = 7;
            // 
            // tabCobro
            // 
            tabCobro.Controls.Add(btnReimprimir);
            tabCobro.Controls.Add(lblCambio);
            tabCobro.Controls.Add(txtImporteRecibido);
            tabCobro.Controls.Add(cboFormaPago);
            tabCobro.Controls.Add(cboMetodoPago);
            tabCobro.Controls.Add(lblResumenCobro);
            tabCobro.Controls.Add(btnConfirmarCobro);
            tabCobro.Controls.Add(cboUsoCFDI);
            tabCobro.Controls.Add(txtRazon);
            tabCobro.Controls.Add(txtRFC);
            tabCobro.Controls.Add(chkFacturarAhora);
            tabCobro.Location = new Point(4, 34);
            tabCobro.Margin = new Padding(4, 5, 4, 5);
            tabCobro.Name = "tabCobro";
            tabCobro.Size = new Size(1135, 712);
            tabCobro.TabIndex = 2;
            tabCobro.Text = "tabCobro";
            tabCobro.UseVisualStyleBackColor = true;
            // 
            // btnReimprimir
            // 
            btnReimprimir.Location = new Point(936, 652);
            btnReimprimir.Margin = new Padding(4, 5, 4, 5);
            btnReimprimir.Name = "btnReimprimir";
            btnReimprimir.Size = new Size(184, 38);
            btnReimprimir.TabIndex = 10;
            btnReimprimir.Text = "Reimprimir ticket";
            btnReimprimir.UseVisualStyleBackColor = true;
            // 
            // lblCambio
            // 
            lblCambio.AutoSize = true;
            lblCambio.Location = new Point(420, 325);
            lblCambio.Margin = new Padding(4, 0, 4, 0);
            lblCambio.Name = "lblCambio";
            lblCambio.Size = new Size(127, 25);
            lblCambio.TabIndex = 9;
            lblCambio.Text = "Cambio: $0.00";
            // 
            // txtImporteRecibido
            // 
            txtImporteRecibido.Location = new Point(29, 320);
            txtImporteRecibido.Margin = new Padding(4, 5, 4, 5);
            txtImporteRecibido.Name = "txtImporteRecibido";
            txtImporteRecibido.Size = new Size(273, 31);
            txtImporteRecibido.TabIndex = 8;
            txtImporteRecibido.TextAlign = HorizontalAlignment.Right;
            txtImporteRecibido.TextChanged += txtImporteRecibido_TextChanged;
            // 
            // cboFormaPago
            // 
            cboFormaPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFormaPago.FormattingEnabled = true;
            cboFormaPago.Location = new Point(311, 203);
            cboFormaPago.Margin = new Padding(4, 5, 4, 5);
            cboFormaPago.Name = "cboFormaPago";
            cboFormaPago.Size = new Size(273, 33);
            cboFormaPago.TabIndex = 7;
            // 
            // cboMetodoPago
            // 
            cboMetodoPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMetodoPago.FormattingEnabled = true;
            cboMetodoPago.Location = new Point(29, 203);
            cboMetodoPago.Margin = new Padding(4, 5, 4, 5);
            cboMetodoPago.Name = "cboMetodoPago";
            cboMetodoPago.Size = new Size(273, 33);
            cboMetodoPago.TabIndex = 6;
            // 
            // lblResumenCobro
            // 
            lblResumenCobro.AutoSize = true;
            lblResumenCobro.Location = new Point(29, 402);
            lblResumenCobro.Margin = new Padding(4, 0, 4, 0);
            lblResumenCobro.Name = "lblResumenCobro";
            lblResumenCobro.Size = new Size(59, 25);
            lblResumenCobro.TabIndex = 5;
            lblResumenCobro.Text = "label1";
            // 
            // btnConfirmarCobro
            // 
            btnConfirmarCobro.Location = new Point(936, 590);
            btnConfirmarCobro.Margin = new Padding(4, 5, 4, 5);
            btnConfirmarCobro.Name = "btnConfirmarCobro";
            btnConfirmarCobro.Size = new Size(184, 38);
            btnConfirmarCobro.TabIndex = 4;
            btnConfirmarCobro.Text = "Confirmar y Cerrar";
            btnConfirmarCobro.UseVisualStyleBackColor = true;
            btnConfirmarCobro.Click += btnConfirmarCobro_Click;
            // 
            // cboUsoCFDI
            // 
            cboUsoCFDI.DropDownStyle = ComboBoxStyle.DropDownList;
            cboUsoCFDI.Enabled = false;
            cboUsoCFDI.FormattingEnabled = true;
            cboUsoCFDI.Location = new Point(594, 97);
            cboUsoCFDI.Margin = new Padding(4, 5, 4, 5);
            cboUsoCFDI.Name = "cboUsoCFDI";
            cboUsoCFDI.Size = new Size(273, 33);
            cboUsoCFDI.TabIndex = 3;
            // 
            // txtRazon
            // 
            txtRazon.Enabled = false;
            txtRazon.Location = new Point(311, 97);
            txtRazon.Margin = new Padding(4, 5, 4, 5);
            txtRazon.Name = "txtRazon";
            txtRazon.Size = new Size(273, 31);
            txtRazon.TabIndex = 2;
            // 
            // txtRFC
            // 
            txtRFC.CharacterCasing = CharacterCasing.Upper;
            txtRFC.Enabled = false;
            txtRFC.Location = new Point(29, 97);
            txtRFC.Margin = new Padding(4, 5, 4, 5);
            txtRFC.Name = "txtRFC";
            txtRFC.Size = new Size(273, 31);
            txtRFC.TabIndex = 1;
            // 
            // chkFacturarAhora
            // 
            chkFacturarAhora.AutoSize = true;
            chkFacturarAhora.Location = new Point(29, 33);
            chkFacturarAhora.Margin = new Padding(4, 5, 4, 5);
            chkFacturarAhora.Name = "chkFacturarAhora";
            chkFacturarAhora.Size = new Size(150, 29);
            chkFacturarAhora.TabIndex = 0;
            chkFacturarAhora.Text = "Facturar ahora";
            chkFacturarAhora.UseVisualStyleBackColor = true;
            // 
            // tabInventario
            // 
            tabInventario.Controls.Add(btnInvLimpiar);
            tabInventario.Controls.Add(btnInvEliminar);
            tabInventario.Controls.Add(btnInvGuardarAux);
            tabInventario.Controls.Add(lblInvTotales);
            tabInventario.Controls.Add(dgvInvCaptura);
            tabInventario.Controls.Add(btnInvAgregar);
            tabInventario.Controls.Add(txtInvCostoKg);
            tabInventario.Controls.Add(lblInvKg);
            tabInventario.Controls.Add(txtInvPesoGr);
            tabInventario.Controls.Add(chkInvSimularBascula);
            tabInventario.Controls.Add(btnInvRefrescar);
            tabInventario.Controls.Add(lbInvArticulos);
            tabInventario.Controls.Add(txtInvBuscar);
            tabInventario.Location = new Point(4, 34);
            tabInventario.Margin = new Padding(4, 5, 4, 5);
            tabInventario.Name = "tabInventario";
            tabInventario.Size = new Size(1135, 712);
            tabInventario.TabIndex = 4;
            tabInventario.Text = "tabInventario";
            tabInventario.UseVisualStyleBackColor = true;
            // 
            // btnInvLimpiar
            // 
            btnInvLimpiar.Location = new Point(856, 235);
            btnInvLimpiar.Margin = new Padding(4, 5, 4, 5);
            btnInvLimpiar.Name = "btnInvLimpiar";
            btnInvLimpiar.Size = new Size(107, 38);
            btnInvLimpiar.TabIndex = 12;
            btnInvLimpiar.Text = "Limpiar";
            btnInvLimpiar.UseVisualStyleBackColor = true;
            // 
            // btnInvEliminar
            // 
            btnInvEliminar.Location = new Point(690, 235);
            btnInvEliminar.Margin = new Padding(4, 5, 4, 5);
            btnInvEliminar.Name = "btnInvEliminar";
            btnInvEliminar.Size = new Size(130, 38);
            btnInvEliminar.TabIndex = 11;
            btnInvEliminar.Text = "Eliminar línea";
            btnInvEliminar.UseVisualStyleBackColor = true;
            btnInvEliminar.Click += btnInvEliminar_Click;
            // 
            // btnInvGuardarAux
            // 
            btnInvGuardarAux.Location = new Point(517, 235);
            btnInvGuardarAux.Margin = new Padding(4, 5, 4, 5);
            btnInvGuardarAux.Name = "btnInvGuardarAux";
            btnInvGuardarAux.Size = new Size(141, 38);
            btnInvGuardarAux.TabIndex = 10;
            btnInvGuardarAux.Text = "Guardar en Aux";
            btnInvGuardarAux.UseVisualStyleBackColor = true;
            // 
            // lblInvTotales
            // 
            lblInvTotales.AutoSize = true;
            lblInvTotales.Location = new Point(544, 188);
            lblInvTotales.Margin = new Padding(4, 0, 4, 0);
            lblInvTotales.Name = "lblInvTotales";
            lblInvTotales.Size = new Size(236, 25);
            lblInvTotales.TabIndex = 9;
            lblInvTotales.Text = "Entradas: 0 Kg: 0.000 $: 0.00";
            // 
            // dgvInvCaptura
            // 
            dgvInvCaptura.AllowUserToAddRows = false;
            dgvInvCaptura.AllowUserToDeleteRows = false;
            dgvInvCaptura.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInvCaptura.Dock = DockStyle.Bottom;
            dgvInvCaptura.Location = new Point(370, 345);
            dgvInvCaptura.Margin = new Padding(4, 5, 4, 5);
            dgvInvCaptura.Name = "dgvInvCaptura";
            dgvInvCaptura.RowHeadersVisible = false;
            dgvInvCaptura.RowHeadersWidth = 62;
            dgvInvCaptura.Size = new Size(765, 367);
            dgvInvCaptura.TabIndex = 8;
            dgvInvCaptura.CellContentClick += dgvInvCaptura_CellContentClick;
            // 
            // btnInvAgregar
            // 
            btnInvAgregar.Location = new Point(380, 182);
            btnInvAgregar.Margin = new Padding(4, 5, 4, 5);
            btnInvAgregar.Name = "btnInvAgregar";
            btnInvAgregar.Size = new Size(143, 38);
            btnInvAgregar.TabIndex = 7;
            btnInvAgregar.Text = "Agregar entrada";
            btnInvAgregar.UseVisualStyleBackColor = true;
            // 
            // txtInvCostoKg
            // 
            txtInvCostoKg.Location = new Point(704, 137);
            txtInvCostoKg.Margin = new Padding(4, 5, 4, 5);
            txtInvCostoKg.Name = "txtInvCostoKg";
            txtInvCostoKg.Size = new Size(141, 31);
            txtInvCostoKg.TabIndex = 6;
            txtInvCostoKg.Text = "0";
            txtInvCostoKg.TextAlign = HorizontalAlignment.Right;
            // 
            // lblInvKg
            // 
            lblInvKg.AutoSize = true;
            lblInvKg.Location = new Point(856, 142);
            lblInvKg.Margin = new Padding(4, 0, 4, 0);
            lblInvKg.Name = "lblInvKg";
            lblInvKg.Size = new Size(81, 25);
            lblInvKg.TabIndex = 5;
            lblInvKg.Text = "0.000 kg";
            // 
            // txtInvPesoGr
            // 
            txtInvPesoGr.Location = new Point(544, 137);
            txtInvPesoGr.Margin = new Padding(4, 5, 4, 5);
            txtInvPesoGr.Name = "txtInvPesoGr";
            txtInvPesoGr.ReadOnly = true;
            txtInvPesoGr.Size = new Size(141, 31);
            txtInvPesoGr.TabIndex = 4;
            txtInvPesoGr.TextAlign = HorizontalAlignment.Right;
            // 
            // chkInvSimularBascula
            // 
            chkInvSimularBascula.AutoSize = true;
            chkInvSimularBascula.Location = new Point(380, 140);
            chkInvSimularBascula.Margin = new Padding(4, 5, 4, 5);
            chkInvSimularBascula.Name = "chkInvSimularBascula";
            chkInvSimularBascula.Size = new Size(161, 29);
            chkInvSimularBascula.TabIndex = 3;
            chkInvSimularBascula.Text = "Simular báscula";
            chkInvSimularBascula.UseVisualStyleBackColor = true;
            // 
            // btnInvRefrescar
            // 
            btnInvRefrescar.Location = new Point(580, 53);
            btnInvRefrescar.Margin = new Padding(4, 5, 4, 5);
            btnInvRefrescar.Name = "btnInvRefrescar";
            btnInvRefrescar.Size = new Size(171, 38);
            btnInvRefrescar.TabIndex = 2;
            btnInvRefrescar.Text = "Refrescar SAE";
            btnInvRefrescar.UseVisualStyleBackColor = true;
            // 
            // lbInvArticulos
            // 
            lbInvArticulos.Dock = DockStyle.Left;
            lbInvArticulos.FormattingEnabled = true;
            lbInvArticulos.IntegralHeight = false;
            lbInvArticulos.ItemHeight = 25;
            lbInvArticulos.Location = new Point(0, 0);
            lbInvArticulos.Margin = new Padding(4, 5, 4, 5);
            lbInvArticulos.Name = "lbInvArticulos";
            lbInvArticulos.Size = new Size(370, 712);
            lbInvArticulos.TabIndex = 1;
            lbInvArticulos.SelectedIndexChanged += lbInvArticulos_SelectedIndexChanged;
            // 
            // txtInvBuscar
            // 
            txtInvBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInvBuscar.Location = new Point(380, 5);
            txtInvBuscar.Margin = new Padding(4, 5, 4, 5);
            txtInvBuscar.Name = "txtInvBuscar";
            txtInvBuscar.Size = new Size(370, 31);
            txtInvBuscar.TabIndex = 0;
            // 
            // tabConfig
            // 
            tabConfig.Controls.Add(btnCfgMeseros);
            tabConfig.Controls.Add(btnCfgMesas);
            tabConfig.Controls.Add(btnGuardarConfig);
            tabConfig.Controls.Add(btnProbarBascula);
            tabConfig.Controls.Add(cboListaPrecios);
            tabConfig.Controls.Add(cboAlmacen);
            tabConfig.Controls.Add(txtPuertoCom);
            tabConfig.Controls.Add(cboImpresora);
            tabConfig.Controls.Add(btnPruebaAux);
            tabConfig.Controls.Add(btnPruebaSae);
            tabConfig.Controls.Add(txtRutaAux);
            tabConfig.Controls.Add(txtRutaSae);
            tabConfig.Location = new Point(4, 34);
            tabConfig.Margin = new Padding(4, 5, 4, 5);
            tabConfig.Name = "tabConfig";
            tabConfig.Size = new Size(1135, 712);
            tabConfig.TabIndex = 3;
            tabConfig.Text = "tabConfig";
            tabConfig.UseVisualStyleBackColor = true;
            // 
            // btnCfgMeseros
            // 
            btnCfgMeseros.Location = new Point(974, 583);
            btnCfgMeseros.Margin = new Padding(4, 5, 4, 5);
            btnCfgMeseros.Name = "btnCfgMeseros";
            btnCfgMeseros.Size = new Size(146, 38);
            btnCfgMeseros.TabIndex = 14;
            btnCfgMeseros.Text = "btnCfgMeseros";
            btnCfgMeseros.UseVisualStyleBackColor = true;
            btnCfgMeseros.Click += btnCfgMeseros_Click;
            // 
            // btnCfgMesas
            // 
            btnCfgMesas.Location = new Point(831, 583);
            btnCfgMesas.Margin = new Padding(4, 5, 4, 5);
            btnCfgMesas.Name = "btnCfgMesas";
            btnCfgMesas.Size = new Size(134, 38);
            btnCfgMesas.TabIndex = 13;
            btnCfgMesas.Text = "btnCfgMesas";
            btnCfgMesas.UseVisualStyleBackColor = true;
            btnCfgMesas.Click += btnCfgMesas_Click;
            // 
            // btnGuardarConfig
            // 
            btnGuardarConfig.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnGuardarConfig.Location = new Point(996, 652);
            btnGuardarConfig.Margin = new Padding(4, 5, 4, 5);
            btnGuardarConfig.Name = "btnGuardarConfig";
            btnGuardarConfig.Size = new Size(107, 38);
            btnGuardarConfig.TabIndex = 12;
            btnGuardarConfig.Text = "Guardar";
            btnGuardarConfig.UseVisualStyleBackColor = true;
            btnGuardarConfig.Click += btnGuardarConfig_Click;
            // 
            // btnProbarBascula
            // 
            btnProbarBascula.Location = new Point(547, 437);
            btnProbarBascula.Margin = new Padding(4, 5, 4, 5);
            btnProbarBascula.Name = "btnProbarBascula";
            btnProbarBascula.Size = new Size(151, 38);
            btnProbarBascula.TabIndex = 11;
            btnProbarBascula.Text = "Probar Báscula";
            btnProbarBascula.UseVisualStyleBackColor = true;
            // 
            // cboListaPrecios
            // 
            cboListaPrecios.FormattingEnabled = true;
            cboListaPrecios.Location = new Point(293, 558);
            cboListaPrecios.Margin = new Padding(4, 5, 4, 5);
            cboListaPrecios.Name = "cboListaPrecios";
            cboListaPrecios.Size = new Size(217, 33);
            cboListaPrecios.TabIndex = 10;
            // 
            // cboAlmacen
            // 
            cboAlmacen.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAlmacen.FormattingEnabled = true;
            cboAlmacen.Location = new Point(11, 558);
            cboAlmacen.Margin = new Padding(4, 5, 4, 5);
            cboAlmacen.Name = "cboAlmacen";
            cboAlmacen.Size = new Size(171, 33);
            cboAlmacen.TabIndex = 9;
            // 
            // txtPuertoCom
            // 
            txtPuertoCom.Location = new Point(293, 437);
            txtPuertoCom.Margin = new Padding(4, 5, 4, 5);
            txtPuertoCom.Name = "txtPuertoCom";
            txtPuertoCom.Size = new Size(217, 31);
            txtPuertoCom.TabIndex = 8;
            txtPuertoCom.Text = "COM1";
            // 
            // cboImpresora
            // 
            cboImpresora.DropDownStyle = ComboBoxStyle.DropDownList;
            cboImpresora.FormattingEnabled = true;
            cboImpresora.Location = new Point(11, 437);
            cboImpresora.Margin = new Padding(4, 5, 4, 5);
            cboImpresora.Name = "cboImpresora";
            cboImpresora.Size = new Size(171, 33);
            cboImpresora.TabIndex = 7;
            // 
            // btnPruebaAux
            // 
            btnPruebaAux.Location = new Point(11, 305);
            btnPruebaAux.Margin = new Padding(4, 5, 4, 5);
            btnPruebaAux.Name = "btnPruebaAux";
            btnPruebaAux.Size = new Size(219, 38);
            btnPruebaAux.TabIndex = 6;
            btnPruebaAux.Text = "Probar conexión Aux";
            btnPruebaAux.UseVisualStyleBackColor = true;
            btnPruebaAux.Click += btnPruebaAux_Click;
            // 
            // btnPruebaSae
            // 
            btnPruebaSae.Location = new Point(11, 120);
            btnPruebaSae.Margin = new Padding(4, 5, 4, 5);
            btnPruebaSae.Name = "btnPruebaSae";
            btnPruebaSae.Size = new Size(219, 38);
            btnPruebaSae.TabIndex = 5;
            btnPruebaSae.Text = "Probar conexión SAE";
            btnPruebaSae.UseVisualStyleBackColor = true;
            btnPruebaSae.Click += btnPruebaSae_Click;
            // 
            // txtRutaAux
            // 
            txtRutaAux.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtRutaAux.Location = new Point(11, 257);
            txtRutaAux.Margin = new Padding(4, 5, 4, 5);
            txtRutaAux.Name = "txtRutaAux";
            txtRutaAux.ReadOnly = true;
            txtRutaAux.Size = new Size(624, 31);
            txtRutaAux.TabIndex = 3;
            txtRutaAux.TextChanged += txtRutaAux_TextChanged;
            // 
            // txtRutaSae
            // 
            txtRutaSae.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtRutaSae.Location = new Point(11, 72);
            txtRutaSae.Margin = new Padding(4, 5, 4, 5);
            txtRutaSae.Name = "txtRutaSae";
            txtRutaSae.ReadOnly = true;
            txtRutaSae.Size = new Size(624, 31);
            txtRutaSae.TabIndex = 2;
            // 
            // btnQuitarLinea
            // 
            btnQuitarLinea.Location = new Point(958, 298);
            btnQuitarLinea.Name = "btnQuitarLinea";
            btnQuitarLinea.Size = new Size(169, 34);
            btnQuitarLinea.TabIndex = 16;
            btnQuitarLinea.Text = "Eliminar platillo";
            btnQuitarLinea.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1143, 750);
            Controls.Add(tabMain);
            Margin = new Padding(4, 5, 4, 5);
            Name = "Form1";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            FormClosing += Form1_FormClosing;
            Load += Form1_Load_1;
            tabMain.ResumeLayout(false);
            tabMesas.ResumeLayout(false);
            tabMesas.PerformLayout();
            statusMain.ResumeLayout(false);
            statusMain.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMesas).EndInit();
            tabPedido.ResumeLayout(false);
            tabPedido.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPedido).EndInit();
            tabCobro.ResumeLayout(false);
            tabCobro.PerformLayout();
            tabInventario.ResumeLayout(false);
            tabInventario.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInvCaptura).EndInit();
            tabConfig.ResumeLayout(false);
            tabConfig.PerformLayout();
            ResumeLayout(false);
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
        private TextBox txtPesoGr;
        private CheckBox chkSimularBascula;
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
    }
}
