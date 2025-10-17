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
            lblCobroMesa = new Label();
            lblCobroTotal = new Label();
            txtCobroEfectivo = new TextBox();
            txtCobroTarjeta = new TextBox();
            txtCobroRef = new TextBox();
            lblCobroCambio = new Label();
            btnCobroConfirmar = new Button();
            btnCobroCancelar = new Button();
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
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1329, 614);
            tabMain.TabIndex = 0;
            // 
            // tabMesas
            // 
            tabMesas.Controls.Add(statusMain);
            tabMesas.Controls.Add(panel1);
            tabMesas.Controls.Add(dgvMesas);
            tabMesas.Location = new Point(4, 24);
            tabMesas.Name = "tabMesas";
            tabMesas.Padding = new Padding(3, 3, 3, 3);
            tabMesas.Size = new Size(1321, 586);
            tabMesas.TabIndex = 0;
            tabMesas.Text = "tabMesas";
            tabMesas.UseVisualStyleBackColor = true;
            // 
            // statusMain
            // 
            statusMain.ImageScalingSize = new Size(24, 24);
            statusMain.Items.AddRange(new ToolStripItem[] { tslSae, tslAux, tslBascula });
            statusMain.Location = new Point(383, 561);
            statusMain.Name = "statusMain";
            statusMain.Size = new Size(935, 22);
            statusMain.TabIndex = 4;
            statusMain.Text = "statusStrip1";
            // 
            // tslSae
            // 
            tslSae.Name = "tslSae";
            tslSae.Size = new Size(54, 17);
            tslSae.Text = "SAE: OFF";
            // 
            // tslAux
            // 
            tslAux.Name = "tslAux";
            tslAux.Size = new Size(55, 17);
            tslAux.Text = "Aux: OFF";
            // 
            // tslBascula
            // 
            tslBascula.Name = "tslBascula";
            tslBascula.Size = new Size(74, 17);
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
            panel1.Location = new Point(383, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(935, 580);
            panel1.TabIndex = 3;
            // 
            // btnLiberarMesa
            // 
            btnLiberarMesa.Location = new Point(168, 350);
            btnLiberarMesa.Name = "btnLiberarMesa";
            btnLiberarMesa.Size = new Size(75, 23);
            btnLiberarMesa.TabIndex = 6;
            btnLiberarMesa.Text = "btnLiberarMesa";
            btnLiberarMesa.UseVisualStyleBackColor = true;
            btnLiberarMesa.Click += btnLiberarMesa_Click;
            // 
            // btnAsignarMesero
            // 
            btnAsignarMesero.Location = new Point(293, 43);
            btnAsignarMesero.Name = "btnAsignarMesero";
            btnAsignarMesero.Size = new Size(75, 23);
            btnAsignarMesero.TabIndex = 5;
            btnAsignarMesero.Text = "Asignar";
            btnAsignarMesero.UseVisualStyleBackColor = true;
            btnAsignarMesero.Click += btnAsignarMesero_Click;
            // 
            // btnAbrirMesa
            // 
            btnAbrirMesa.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAbrirMesa.Location = new Point(676, 332);
            btnAbrirMesa.Name = "btnAbrirMesa";
            btnAbrirMesa.Size = new Size(121, 23);
            btnAbrirMesa.TabIndex = 1;
            btnAbrirMesa.Text = "Atender Mesa";
            btnAbrirMesa.UseVisualStyleBackColor = true;
            btnAbrirMesa.Click += btnAbrirMesa_Click;
            // 
            // lblMesaSel
            // 
            lblMesaSel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMesaSel.AutoSize = true;
            lblMesaSel.Location = new Point(713, 25);
            lblMesaSel.Name = "lblMesaSel";
            lblMesaSel.Size = new Size(125, 15);
            lblMesaSel.TabIndex = 0;
            lblMesaSel.Text = "Sin mesa seleccionada";
            // 
            // cboMesero
            // 
            cboMesero.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cboMesero.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMesero.FormattingEnabled = true;
            cboMesero.Location = new Point(676, 43);
            cboMesero.Name = "cboMesero";
            cboMesero.Size = new Size(121, 23);
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
            dgvMesas.Location = new Point(3, 3);
            dgvMesas.MultiSelect = false;
            dgvMesas.Name = "dgvMesas";
            dgvMesas.ReadOnly = true;
            dgvMesas.RowHeadersVisible = false;
            dgvMesas.RowHeadersWidth = 62;
            dgvMesas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMesas.Size = new Size(380, 580);
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
            tabPedido.Location = new Point(4, 24);
            tabPedido.Name = "tabPedido";
            tabPedido.Padding = new Padding(3, 3, 3, 3);
            tabPedido.Size = new Size(1321, 586);
            tabPedido.TabIndex = 1;
            tabPedido.Text = "tabPedido";
            tabPedido.UseVisualStyleBackColor = true;
            // 
            // dgvReceta
            // 
            dgvReceta.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReceta.Location = new Point(708, 368);
            dgvReceta.Margin = new Padding(2, 2, 2, 2);
            dgvReceta.Name = "dgvReceta";
            dgvReceta.RowHeadersWidth = 62;
            dgvReceta.Size = new Size(610, 217);
            dgvReceta.TabIndex = 17;
            // 
            // btnQuitarLinea
            // 
            btnQuitarLinea.Location = new Point(411, 343);
            btnQuitarLinea.Margin = new Padding(2, 2, 2, 2);
            btnQuitarLinea.Name = "btnQuitarLinea";
            btnQuitarLinea.Size = new Size(118, 20);
            btnQuitarLinea.TabIndex = 16;
            btnQuitarLinea.Text = "Eliminar platillo";
            btnQuitarLinea.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 3);
            label1.Name = "label1";
            label1.Size = new Size(51, 15);
            label1.TabIndex = 15;
            label1.Text = "Buscar...";
            // 
            // txtBuscarPlatillo
            // 
            txtBuscarPlatillo.Location = new Point(3, 18);
            txtBuscarPlatillo.Name = "txtBuscarPlatillo";
            txtBuscarPlatillo.Size = new Size(280, 23);
            txtBuscarPlatillo.TabIndex = 14;
            txtBuscarPlatillo.TextChanged += txtBuscarPlatillo_TextChanged;
            // 
            // btnIrCobro
            // 
            btnIrCobro.Location = new Point(1095, 76);
            btnIrCobro.Name = "btnIrCobro";
            btnIrCobro.Size = new Size(75, 23);
            btnIrCobro.TabIndex = 13;
            btnIrCobro.Text = "Cobrar";
            btnIrCobro.UseVisualStyleBackColor = true;
            btnIrCobro.Click += btnIrCobro_Click;
            // 
            // lblTotales
            // 
            lblTotales.AutoSize = true;
            lblTotales.Location = new Point(1035, 50);
            lblTotales.Name = "lblTotales";
            lblTotales.Size = new Size(76, 15);
            lblTotales.TabIndex = 12;
            lblTotales.Text = "                       ";
            // 
            // dgvPedido
            // 
            dgvPedido.AllowUserToAddRows = false;
            dgvPedido.AllowUserToDeleteRows = false;
            dgvPedido.AllowUserToResizeColumns = false;
            dgvPedido.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPedido.Location = new Point(281, 368);
            dgvPedido.Name = "dgvPedido";
            dgvPedido.RowHeadersVisible = false;
            dgvPedido.RowHeadersWidth = 62;
            dgvPedido.Size = new Size(421, 217);
            dgvPedido.TabIndex = 11;
            // 
            // btnAgregarLinea
            // 
            btnAgregarLinea.Location = new Point(287, 43);
            btnAgregarLinea.Name = "btnAgregarLinea";
            btnAgregarLinea.Size = new Size(109, 23);
            btnAgregarLinea.TabIndex = 10;
            btnAgregarLinea.Text = "Agregar Platillo";
            btnAgregarLinea.UseVisualStyleBackColor = true;
            btnAgregarLinea.Click += btnAgregarLinea_Click;
            // 
            // lbPlatillos
            // 
            lbPlatillos.FormattingEnabled = true;
            lbPlatillos.IntegralHeight = false;
            lbPlatillos.ItemHeight = 15;
            lbPlatillos.Location = new Point(3, 43);
            lbPlatillos.Name = "lbPlatillos";
            lbPlatillos.Size = new Size(280, 548);
            lbPlatillos.TabIndex = 7;
            // 
            // tabCobro
            // 
            tabCobro.Controls.Add(btnCobroCancelar);
            tabCobro.Controls.Add(btnCobroConfirmar);
            tabCobro.Controls.Add(lblCobroCambio);
            tabCobro.Controls.Add(txtCobroRef);
            tabCobro.Controls.Add(txtCobroTarjeta);
            tabCobro.Controls.Add(txtCobroEfectivo);
            tabCobro.Controls.Add(lblCobroTotal);
            tabCobro.Controls.Add(lblCobroMesa);
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
            tabCobro.Location = new Point(4, 24);
            tabCobro.Name = "tabCobro";
            tabCobro.Size = new Size(1321, 586);
            tabCobro.TabIndex = 2;
            tabCobro.Text = "tabCobro";
            tabCobro.UseVisualStyleBackColor = true;
            // 
            // btnReimprimir
            // 
            btnReimprimir.Location = new Point(1157, 549);
            btnReimprimir.Name = "btnReimprimir";
            btnReimprimir.Size = new Size(129, 23);
            btnReimprimir.TabIndex = 10;
            btnReimprimir.Text = "Reimprimir ticket";
            btnReimprimir.UseVisualStyleBackColor = true;
            // 
            // lblCambio
            // 
            lblCambio.AutoSize = true;
            lblCambio.Location = new Point(563, 328);
            lblCambio.Name = "lblCambio";
            lblCambio.Size = new Size(82, 15);
            lblCambio.TabIndex = 9;
            lblCambio.Text = "Cambio: $0.00";
            // 
            // txtImporteRecibido
            // 
            txtImporteRecibido.Location = new Point(289, 325);
            txtImporteRecibido.Name = "txtImporteRecibido";
            txtImporteRecibido.Size = new Size(192, 23);
            txtImporteRecibido.TabIndex = 8;
            txtImporteRecibido.TextAlign = HorizontalAlignment.Right;
            txtImporteRecibido.TextChanged += txtImporteRecibido_TextChanged;
            // 
            // cboFormaPago
            // 
            cboFormaPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFormaPago.FormattingEnabled = true;
            cboFormaPago.Location = new Point(487, 255);
            cboFormaPago.Name = "cboFormaPago";
            cboFormaPago.Size = new Size(192, 23);
            cboFormaPago.TabIndex = 7;
            // 
            // cboMetodoPago
            // 
            cboMetodoPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMetodoPago.FormattingEnabled = true;
            cboMetodoPago.Location = new Point(289, 255);
            cboMetodoPago.Name = "cboMetodoPago";
            cboMetodoPago.Size = new Size(192, 23);
            cboMetodoPago.TabIndex = 6;
            // 
            // lblResumenCobro
            // 
            lblResumenCobro.AutoSize = true;
            lblResumenCobro.Location = new Point(289, 374);
            lblResumenCobro.Name = "lblResumenCobro";
            lblResumenCobro.Size = new Size(38, 15);
            lblResumenCobro.TabIndex = 5;
            lblResumenCobro.Text = "label1";
            // 
            // btnConfirmarCobro
            // 
            btnConfirmarCobro.Location = new Point(1157, 509);
            btnConfirmarCobro.Name = "btnConfirmarCobro";
            btnConfirmarCobro.Size = new Size(129, 23);
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
            cboUsoCFDI.Location = new Point(685, 191);
            cboUsoCFDI.Name = "cboUsoCFDI";
            cboUsoCFDI.Size = new Size(192, 23);
            cboUsoCFDI.TabIndex = 3;
            // 
            // txtRazon
            // 
            txtRazon.Enabled = false;
            txtRazon.Location = new Point(487, 191);
            txtRazon.Name = "txtRazon";
            txtRazon.Size = new Size(192, 23);
            txtRazon.TabIndex = 2;
            // 
            // txtRFC
            // 
            txtRFC.CharacterCasing = CharacterCasing.Upper;
            txtRFC.Enabled = false;
            txtRFC.Location = new Point(289, 191);
            txtRFC.Name = "txtRFC";
            txtRFC.Size = new Size(192, 23);
            txtRFC.TabIndex = 1;
            // 
            // chkFacturarAhora
            // 
            chkFacturarAhora.AutoSize = true;
            chkFacturarAhora.Location = new Point(289, 153);
            chkFacturarAhora.Name = "chkFacturarAhora";
            chkFacturarAhora.Size = new Size(102, 19);
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
            tabInventario.Location = new Point(4, 24);
            tabInventario.Name = "tabInventario";
            tabInventario.Size = new Size(1321, 586);
            tabInventario.TabIndex = 4;
            tabInventario.Text = "tabInventario";
            tabInventario.UseVisualStyleBackColor = true;
            // 
            // btnInvLimpiar
            // 
            btnInvLimpiar.Location = new Point(599, 141);
            btnInvLimpiar.Name = "btnInvLimpiar";
            btnInvLimpiar.Size = new Size(75, 23);
            btnInvLimpiar.TabIndex = 12;
            btnInvLimpiar.Text = "Limpiar";
            btnInvLimpiar.UseVisualStyleBackColor = true;
            // 
            // btnInvEliminar
            // 
            btnInvEliminar.Location = new Point(483, 141);
            btnInvEliminar.Name = "btnInvEliminar";
            btnInvEliminar.Size = new Size(91, 23);
            btnInvEliminar.TabIndex = 11;
            btnInvEliminar.Text = "Eliminar línea";
            btnInvEliminar.UseVisualStyleBackColor = true;
            btnInvEliminar.Click += btnInvEliminar_Click;
            // 
            // btnInvGuardarAux
            // 
            btnInvGuardarAux.Location = new Point(362, 141);
            btnInvGuardarAux.Name = "btnInvGuardarAux";
            btnInvGuardarAux.Size = new Size(99, 23);
            btnInvGuardarAux.TabIndex = 10;
            btnInvGuardarAux.Text = "Guardar en Aux";
            btnInvGuardarAux.UseVisualStyleBackColor = true;
            // 
            // lblInvTotales
            // 
            lblInvTotales.AutoSize = true;
            lblInvTotales.Location = new Point(381, 113);
            lblInvTotales.Name = "lblInvTotales";
            lblInvTotales.Size = new Size(150, 15);
            lblInvTotales.TabIndex = 9;
            lblInvTotales.Text = "Entradas: 0 Kg: 0.000 $: 0.00";
            // 
            // dgvInvCaptura
            // 
            dgvInvCaptura.AllowUserToAddRows = false;
            dgvInvCaptura.AllowUserToDeleteRows = false;
            dgvInvCaptura.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInvCaptura.Dock = DockStyle.Bottom;
            dgvInvCaptura.Location = new Point(260, 366);
            dgvInvCaptura.Name = "dgvInvCaptura";
            dgvInvCaptura.RowHeadersVisible = false;
            dgvInvCaptura.RowHeadersWidth = 62;
            dgvInvCaptura.Size = new Size(1061, 220);
            dgvInvCaptura.TabIndex = 8;
            dgvInvCaptura.CellContentClick += dgvInvCaptura_CellContentClick;
            // 
            // btnInvAgregar
            // 
            btnInvAgregar.Location = new Point(266, 109);
            btnInvAgregar.Name = "btnInvAgregar";
            btnInvAgregar.Size = new Size(100, 23);
            btnInvAgregar.TabIndex = 7;
            btnInvAgregar.Text = "Agregar entrada";
            btnInvAgregar.UseVisualStyleBackColor = true;
            // 
            // txtInvCostoKg
            // 
            txtInvCostoKg.Location = new Point(493, 82);
            txtInvCostoKg.Name = "txtInvCostoKg";
            txtInvCostoKg.Size = new Size(100, 23);
            txtInvCostoKg.TabIndex = 6;
            txtInvCostoKg.Text = "0";
            txtInvCostoKg.TextAlign = HorizontalAlignment.Right;
            // 
            // lblInvKg
            // 
            lblInvKg.AutoSize = true;
            lblInvKg.Location = new Point(599, 85);
            lblInvKg.Name = "lblInvKg";
            lblInvKg.Size = new Size(50, 15);
            lblInvKg.TabIndex = 5;
            lblInvKg.Text = "0.000 kg";
            // 
            // txtInvPesoGr
            // 
            txtInvPesoGr.Location = new Point(381, 82);
            txtInvPesoGr.Name = "txtInvPesoGr";
            txtInvPesoGr.ReadOnly = true;
            txtInvPesoGr.Size = new Size(100, 23);
            txtInvPesoGr.TabIndex = 4;
            txtInvPesoGr.TextAlign = HorizontalAlignment.Right;
            // 
            // chkInvSimularBascula
            // 
            chkInvSimularBascula.AutoSize = true;
            chkInvSimularBascula.Location = new Point(266, 84);
            chkInvSimularBascula.Name = "chkInvSimularBascula";
            chkInvSimularBascula.Size = new Size(109, 19);
            chkInvSimularBascula.TabIndex = 3;
            chkInvSimularBascula.Text = "Simular báscula";
            chkInvSimularBascula.UseVisualStyleBackColor = true;
            // 
            // btnInvRefrescar
            // 
            btnInvRefrescar.Location = new Point(406, 32);
            btnInvRefrescar.Name = "btnInvRefrescar";
            btnInvRefrescar.Size = new Size(120, 23);
            btnInvRefrescar.TabIndex = 2;
            btnInvRefrescar.Text = "Refrescar SAE";
            btnInvRefrescar.UseVisualStyleBackColor = true;
            // 
            // lbInvArticulos
            // 
            lbInvArticulos.Dock = DockStyle.Left;
            lbInvArticulos.FormattingEnabled = true;
            lbInvArticulos.IntegralHeight = false;
            lbInvArticulos.ItemHeight = 15;
            lbInvArticulos.Location = new Point(0, 0);
            lbInvArticulos.Name = "lbInvArticulos";
            lbInvArticulos.Size = new Size(260, 586);
            lbInvArticulos.TabIndex = 1;
            lbInvArticulos.SelectedIndexChanged += lbInvArticulos_SelectedIndexChanged;
            // 
            // txtInvBuscar
            // 
            txtInvBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInvBuscar.Location = new Point(266, 3);
            txtInvBuscar.Name = "txtInvBuscar";
            txtInvBuscar.Size = new Size(260, 23);
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
            tabConfig.Location = new Point(4, 24);
            tabConfig.Name = "tabConfig";
            tabConfig.Size = new Size(1321, 586);
            tabConfig.TabIndex = 3;
            tabConfig.Text = "tabConfig";
            tabConfig.UseVisualStyleBackColor = true;
            // 
            // btnCfgMeseros
            // 
            btnCfgMeseros.Location = new Point(682, 350);
            btnCfgMeseros.Name = "btnCfgMeseros";
            btnCfgMeseros.Size = new Size(102, 23);
            btnCfgMeseros.TabIndex = 14;
            btnCfgMeseros.Text = "btnCfgMeseros";
            btnCfgMeseros.UseVisualStyleBackColor = true;
            btnCfgMeseros.Click += btnCfgMeseros_Click;
            // 
            // btnCfgMesas
            // 
            btnCfgMesas.Location = new Point(582, 350);
            btnCfgMesas.Name = "btnCfgMesas";
            btnCfgMesas.Size = new Size(94, 23);
            btnCfgMesas.TabIndex = 13;
            btnCfgMesas.Text = "btnCfgMesas";
            btnCfgMesas.UseVisualStyleBackColor = true;
            btnCfgMesas.Click += btnCfgMesas_Click;
            // 
            // btnGuardarConfig
            // 
            btnGuardarConfig.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnGuardarConfig.Location = new Point(697, 391);
            btnGuardarConfig.Name = "btnGuardarConfig";
            btnGuardarConfig.Size = new Size(75, 23);
            btnGuardarConfig.TabIndex = 12;
            btnGuardarConfig.Text = "Guardar";
            btnGuardarConfig.UseVisualStyleBackColor = true;
            btnGuardarConfig.Click += btnGuardarConfig_Click;
            // 
            // btnProbarBascula
            // 
            btnProbarBascula.Location = new Point(383, 262);
            btnProbarBascula.Name = "btnProbarBascula";
            btnProbarBascula.Size = new Size(106, 23);
            btnProbarBascula.TabIndex = 11;
            btnProbarBascula.Text = "Probar Báscula";
            btnProbarBascula.UseVisualStyleBackColor = true;
            // 
            // cboListaPrecios
            // 
            cboListaPrecios.FormattingEnabled = true;
            cboListaPrecios.Location = new Point(205, 335);
            cboListaPrecios.Name = "cboListaPrecios";
            cboListaPrecios.Size = new Size(153, 23);
            cboListaPrecios.TabIndex = 10;
            // 
            // cboAlmacen
            // 
            cboAlmacen.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAlmacen.FormattingEnabled = true;
            cboAlmacen.Location = new Point(8, 335);
            cboAlmacen.Name = "cboAlmacen";
            cboAlmacen.Size = new Size(121, 23);
            cboAlmacen.TabIndex = 9;
            // 
            // txtPuertoCom
            // 
            txtPuertoCom.Location = new Point(205, 262);
            txtPuertoCom.Name = "txtPuertoCom";
            txtPuertoCom.Size = new Size(153, 23);
            txtPuertoCom.TabIndex = 8;
            txtPuertoCom.Text = "COM1";
            // 
            // cboImpresora
            // 
            cboImpresora.DropDownStyle = ComboBoxStyle.DropDownList;
            cboImpresora.FormattingEnabled = true;
            cboImpresora.Location = new Point(8, 262);
            cboImpresora.Name = "cboImpresora";
            cboImpresora.Size = new Size(121, 23);
            cboImpresora.TabIndex = 7;
            // 
            // btnPruebaAux
            // 
            btnPruebaAux.Location = new Point(8, 183);
            btnPruebaAux.Name = "btnPruebaAux";
            btnPruebaAux.Size = new Size(153, 23);
            btnPruebaAux.TabIndex = 6;
            btnPruebaAux.Text = "Probar conexión Aux";
            btnPruebaAux.UseVisualStyleBackColor = true;
            btnPruebaAux.Click += btnPruebaAux_Click;
            // 
            // btnPruebaSae
            // 
            btnPruebaSae.Location = new Point(8, 72);
            btnPruebaSae.Name = "btnPruebaSae";
            btnPruebaSae.Size = new Size(153, 23);
            btnPruebaSae.TabIndex = 5;
            btnPruebaSae.Text = "Probar conexión SAE";
            btnPruebaSae.UseVisualStyleBackColor = true;
            btnPruebaSae.Click += btnPruebaSae_Click;
            // 
            // txtRutaAux
            // 
            txtRutaAux.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtRutaAux.Location = new Point(8, 154);
            txtRutaAux.Name = "txtRutaAux";
            txtRutaAux.ReadOnly = true;
            txtRutaAux.Size = new Size(438, 23);
            txtRutaAux.TabIndex = 3;
            txtRutaAux.TextChanged += txtRutaAux_TextChanged;
            // 
            // txtRutaSae
            // 
            txtRutaSae.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtRutaSae.Location = new Point(8, 43);
            txtRutaSae.Name = "txtRutaSae";
            txtRutaSae.ReadOnly = true;
            txtRutaSae.Size = new Size(438, 23);
            txtRutaSae.TabIndex = 2;
            // 
            // lblCobroMesa
            // 
            lblCobroMesa.AutoSize = true;
            lblCobroMesa.Location = new Point(100, 49);
            lblCobroMesa.Name = "lblCobroMesa";
            lblCobroMesa.Size = new Size(45, 15);
            lblCobroMesa.TabIndex = 11;
            lblCobroMesa.Text = "Mesa X";
            // 
            // lblCobroTotal
            // 
            lblCobroTotal.AutoSize = true;
            lblCobroTotal.Location = new Point(105, 85);
            lblCobroTotal.Name = "lblCobroTotal";
            lblCobroTotal.Size = new Size(65, 15);
            lblCobroTotal.TabIndex = 12;
            lblCobroTotal.Text = "Total: $0.00";
            // 
            // txtCobroEfectivo
            // 
            txtCobroEfectivo.Location = new Point(240, 50);
            txtCobroEfectivo.Name = "txtCobroEfectivo";
            txtCobroEfectivo.Size = new Size(100, 23);
            txtCobroEfectivo.TabIndex = 13;
            // 
            // txtCobroTarjeta
            // 
            txtCobroTarjeta.Location = new Point(244, 96);
            txtCobroTarjeta.Name = "txtCobroTarjeta";
            txtCobroTarjeta.Size = new Size(100, 23);
            txtCobroTarjeta.TabIndex = 14;
            // 
            // txtCobroRef
            // 
            txtCobroRef.Location = new Point(452, 48);
            txtCobroRef.Name = "txtCobroRef";
            txtCobroRef.Size = new Size(100, 23);
            txtCobroRef.TabIndex = 15;
            // 
            // lblCobroCambio
            // 
            lblCobroCambio.AutoSize = true;
            lblCobroCambio.Location = new Point(705, 41);
            lblCobroCambio.Name = "lblCobroCambio";
            lblCobroCambio.Size = new Size(82, 15);
            lblCobroCambio.TabIndex = 16;
            lblCobroCambio.Text = "Cambio: $0.00";
            // 
            // btnCobroConfirmar
            // 
            btnCobroConfirmar.Location = new Point(949, 59);
            btnCobroConfirmar.Name = "btnCobroConfirmar";
            btnCobroConfirmar.Size = new Size(75, 23);
            btnCobroConfirmar.TabIndex = 17;
            btnCobroConfirmar.Text = "Cobrar";
            btnCobroConfirmar.UseVisualStyleBackColor = true;
            // 
            // btnCobroCancelar
            // 
            btnCobroCancelar.Location = new Point(965, 112);
            btnCobroCancelar.Name = "btnCobroCancelar";
            btnCobroCancelar.Size = new Size(75, 23);
            btnCobroCancelar.TabIndex = 18;
            btnCobroCancelar.Text = "Regresar";
            btnCobroCancelar.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1329, 614);
            Controls.Add(tabMain);
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
            ((System.ComponentModel.ISupportInitialize)dgvReceta).EndInit();
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
    }
}
