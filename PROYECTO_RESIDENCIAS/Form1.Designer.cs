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
            lblMesaTiempo = new Label();
            btnReabrirMesa = new Button();
            btnCerrarMesa = new Button();
            btnTransferirMesa = new Button();
            btnPrecuentaMesa = new Button();
            btnAsignarMesero = new Button();
            btnAbrirMesa = new Button();
            lblMesaSel = new Label();
            cboMesero = new ComboBox();
            dgvMesas = new DataGridView();
            tabPedido = new TabPage();
            btnNotasPartida = new Button();
            btnDividirLinea = new Button();
            btnDuplicarLinea = new Button();
            chkAutoAgregarPesables = new CheckBox();
            lblPesoStatus = new Label();
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
            btnInvAplicarCostoTodos = new Button();
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
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(800, 450);
            tabMain.TabIndex = 0;
            // 
            // tabMesas
            // 
            tabMesas.Controls.Add(statusMain);
            tabMesas.Controls.Add(panel1);
            tabMesas.Controls.Add(dgvMesas);
            tabMesas.Location = new Point(4, 24);
            tabMesas.Name = "tabMesas";
            tabMesas.Padding = new Padding(3);
            tabMesas.Size = new Size(792, 422);
            tabMesas.TabIndex = 0;
            tabMesas.Text = "tabMesas";
            tabMesas.UseVisualStyleBackColor = true;
            // 
            // statusMain
            // 
            statusMain.Items.AddRange(new ToolStripItem[] { tslSae, tslAux, tslBascula });
            statusMain.Location = new Point(383, 397);
            statusMain.Name = "statusMain";
            statusMain.Size = new Size(406, 22);
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
            panel1.Controls.Add(lblMesaTiempo);
            panel1.Controls.Add(btnReabrirMesa);
            panel1.Controls.Add(btnCerrarMesa);
            panel1.Controls.Add(btnTransferirMesa);
            panel1.Controls.Add(btnPrecuentaMesa);
            panel1.Controls.Add(btnAsignarMesero);
            panel1.Controls.Add(btnAbrirMesa);
            panel1.Controls.Add(lblMesaSel);
            panel1.Controls.Add(cboMesero);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(383, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(406, 416);
            panel1.TabIndex = 3;
            // 
            // lblMesaTiempo
            // 
            lblMesaTiempo.AutoSize = true;
            lblMesaTiempo.Location = new Point(247, 130);
            lblMesaTiempo.Name = "lblMesaTiempo";
            lblMesaTiempo.Size = new Size(34, 15);
            lblMesaTiempo.TabIndex = 10;
            lblMesaTiempo.Text = "00:00";
            // 
            // btnReabrirMesa
            // 
            btnReabrirMesa.Location = new Point(306, 165);
            btnReabrirMesa.Name = "btnReabrirMesa";
            btnReabrirMesa.Size = new Size(75, 23);
            btnReabrirMesa.TabIndex = 9;
            btnReabrirMesa.Text = "Reabrir";
            btnReabrirMesa.UseVisualStyleBackColor = true;
            // 
            // btnCerrarMesa
            // 
            btnCerrarMesa.Location = new Point(306, 122);
            btnCerrarMesa.Name = "btnCerrarMesa";
            btnCerrarMesa.Size = new Size(75, 23);
            btnCerrarMesa.TabIndex = 8;
            btnCerrarMesa.Text = "Cerrar Mesa";
            btnCerrarMesa.UseVisualStyleBackColor = true;
            // 
            // btnTransferirMesa
            // 
            btnTransferirMesa.Location = new Point(306, 78);
            btnTransferirMesa.Name = "btnTransferirMesa";
            btnTransferirMesa.Size = new Size(75, 23);
            btnTransferirMesa.TabIndex = 7;
            btnTransferirMesa.Text = "Transferir";
            btnTransferirMesa.UseVisualStyleBackColor = true;
            // 
            // btnPrecuentaMesa
            // 
            btnPrecuentaMesa.Location = new Point(306, 34);
            btnPrecuentaMesa.Name = "btnPrecuentaMesa";
            btnPrecuentaMesa.Size = new Size(75, 23);
            btnPrecuentaMesa.TabIndex = 6;
            btnPrecuentaMesa.Text = "Precuenta";
            btnPrecuentaMesa.UseVisualStyleBackColor = true;
            // 
            // btnAsignarMesero
            // 
            btnAsignarMesero.Location = new Point(146, 35);
            btnAsignarMesero.Name = "btnAsignarMesero";
            btnAsignarMesero.Size = new Size(75, 23);
            btnAsignarMesero.TabIndex = 5;
            btnAsignarMesero.Text = "Asignar";
            btnAsignarMesero.UseVisualStyleBackColor = true;
            // 
            // btnAbrirMesa
            // 
            btnAbrirMesa.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAbrirMesa.Location = new Point(6, 78);
            btnAbrirMesa.Name = "btnAbrirMesa";
            btnAbrirMesa.Size = new Size(121, 23);
            btnAbrirMesa.TabIndex = 1;
            btnAbrirMesa.Text = "Atender Mesa";
            btnAbrirMesa.UseVisualStyleBackColor = true;
            // 
            // lblMesaSel
            // 
            lblMesaSel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblMesaSel.AutoSize = true;
            lblMesaSel.Location = new Point(6, 17);
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
            cboMesero.Location = new Point(6, 35);
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
            dgvMesas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMesas.Size = new Size(380, 416);
            dgvMesas.TabIndex = 0;
            // 
            // tabPedido
            // 
            tabPedido.Controls.Add(btnNotasPartida);
            tabPedido.Controls.Add(btnDividirLinea);
            tabPedido.Controls.Add(btnDuplicarLinea);
            tabPedido.Controls.Add(chkAutoAgregarPesables);
            tabPedido.Controls.Add(lblPesoStatus);
            tabPedido.Controls.Add(txtBuscarPlatillo);
            tabPedido.Controls.Add(btnIrCobro);
            tabPedido.Controls.Add(lblTotales);
            tabPedido.Controls.Add(dgvPedido);
            tabPedido.Controls.Add(btnAgregarLinea);
            tabPedido.Controls.Add(txtPesoGr);
            tabPedido.Controls.Add(chkSimularBascula);
            tabPedido.Controls.Add(lbPlatillos);
            tabPedido.Location = new Point(4, 24);
            tabPedido.Name = "tabPedido";
            tabPedido.Padding = new Padding(3);
            tabPedido.Size = new Size(792, 422);
            tabPedido.TabIndex = 1;
            tabPedido.Text = "tabPedido";
            tabPedido.UseVisualStyleBackColor = true;
            // 
            // btnNotasPartida
            // 
            btnNotasPartida.Location = new Point(672, 151);
            btnNotasPartida.Name = "btnNotasPartida";
            btnNotasPartida.Size = new Size(75, 23);
            btnNotasPartida.TabIndex = 19;
            btnNotasPartida.Text = "Notas";
            btnNotasPartida.UseVisualStyleBackColor = true;
            // 
            // btnDividirLinea
            // 
            btnDividirLinea.Location = new Point(289, 165);
            btnDividirLinea.Name = "btnDividirLinea";
            btnDividirLinea.Size = new Size(109, 23);
            btnDividirLinea.TabIndex = 18;
            btnDividirLinea.Text = "Dividir";
            btnDividirLinea.UseVisualStyleBackColor = true;
            // 
            // btnDuplicarLinea
            // 
            btnDuplicarLinea.Location = new Point(289, 136);
            btnDuplicarLinea.Name = "btnDuplicarLinea";
            btnDuplicarLinea.Size = new Size(109, 23);
            btnDuplicarLinea.TabIndex = 17;
            btnDuplicarLinea.Text = "Duplicar";
            btnDuplicarLinea.UseVisualStyleBackColor = true;
            // 
            // chkAutoAgregarPesables
            // 
            chkAutoAgregarPesables.AutoSize = true;
            chkAutoAgregarPesables.Location = new Point(464, 52);
            chkAutoAgregarPesables.Name = "chkAutoAgregarPesables";
            chkAutoAgregarPesables.Size = new Size(145, 19);
            chkAutoAgregarPesables.TabIndex = 16;
            chkAutoAgregarPesables.Text = "Auto-agregar pesables";
            chkAutoAgregarPesables.UseVisualStyleBackColor = true;
            // 
            // lblPesoStatus
            // 
            lblPesoStatus.AutoSize = true;
            lblPesoStatus.ForeColor = Color.DarkRed;
            lblPesoStatus.Location = new Point(404, 56);
            lblPesoStatus.Name = "lblPesoStatus";
            lblPesoStatus.Size = new Size(54, 15);
            lblPesoStatus.TabIndex = 15;
            lblPesoStatus.Text = "Inestable";
            // 
            // txtBuscarPlatillo
            // 
            txtBuscarPlatillo.Dock = DockStyle.Left;
            txtBuscarPlatillo.Location = new Point(283, 3);
            txtBuscarPlatillo.Name = "txtBuscarPlatillo";
            txtBuscarPlatillo.Size = new Size(260, 23);
            txtBuscarPlatillo.TabIndex = 14;
            txtBuscarPlatillo.Text = "Buscar";
            txtBuscarPlatillo.TextChanged += txtBuscarPlatillo_TextChanged;
            // 
            // btnIrCobro
            // 
            btnIrCobro.Location = new Point(672, 98);
            btnIrCobro.Name = "btnIrCobro";
            btnIrCobro.Size = new Size(75, 23);
            btnIrCobro.TabIndex = 13;
            btnIrCobro.Text = "Cobrar";
            btnIrCobro.UseVisualStyleBackColor = true;
            // 
            // lblTotales
            // 
            lblTotales.AutoSize = true;
            lblTotales.Location = new Point(692, 61);
            lblTotales.Name = "lblTotales";
            lblTotales.Size = new Size(38, 15);
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
            dgvPedido.Location = new Point(283, 199);
            dgvPedido.Name = "dgvPedido";
            dgvPedido.RowHeadersVisible = false;
            dgvPedido.Size = new Size(506, 220);
            dgvPedido.TabIndex = 11;
            // 
            // btnAgregarLinea
            // 
            btnAgregarLinea.Location = new Point(289, 107);
            btnAgregarLinea.Name = "btnAgregarLinea";
            btnAgregarLinea.Size = new Size(109, 23);
            btnAgregarLinea.TabIndex = 10;
            btnAgregarLinea.Text = "Agregar";
            btnAgregarLinea.UseVisualStyleBackColor = true;
            // 
            // txtPesoGr
            // 
            txtPesoGr.Location = new Point(289, 53);
            txtPesoGr.Name = "txtPesoGr";
            txtPesoGr.ReadOnly = true;
            txtPesoGr.Size = new Size(109, 23);
            txtPesoGr.TabIndex = 9;
            txtPesoGr.TextAlign = HorizontalAlignment.Right;
            // 
            // chkSimularBascula
            // 
            chkSimularBascula.AutoSize = true;
            chkSimularBascula.Location = new Point(289, 82);
            chkSimularBascula.Name = "chkSimularBascula";
            chkSimularBascula.Size = new Size(109, 19);
            chkSimularBascula.TabIndex = 8;
            chkSimularBascula.Text = "Simular Bascula";
            chkSimularBascula.UseVisualStyleBackColor = true;
            // 
            // lbPlatillos
            // 
            lbPlatillos.Dock = DockStyle.Left;
            lbPlatillos.FormattingEnabled = true;
            lbPlatillos.IntegralHeight = false;
            lbPlatillos.ItemHeight = 15;
            lbPlatillos.Location = new Point(3, 3);
            lbPlatillos.Name = "lbPlatillos";
            lbPlatillos.Size = new Size(280, 416);
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
            tabCobro.Location = new Point(4, 24);
            tabCobro.Name = "tabCobro";
            tabCobro.Size = new Size(792, 422);
            tabCobro.TabIndex = 2;
            tabCobro.Text = "tabCobro";
            tabCobro.UseVisualStyleBackColor = true;
            // 
            // btnReimprimir
            // 
            btnReimprimir.Location = new Point(655, 391);
            btnReimprimir.Name = "btnReimprimir";
            btnReimprimir.Size = new Size(129, 23);
            btnReimprimir.TabIndex = 10;
            btnReimprimir.Text = "Reimprimir ticket";
            btnReimprimir.UseVisualStyleBackColor = true;
            // 
            // lblCambio
            // 
            lblCambio.AutoSize = true;
            lblCambio.Location = new Point(294, 195);
            lblCambio.Name = "lblCambio";
            lblCambio.Size = new Size(82, 15);
            lblCambio.TabIndex = 9;
            lblCambio.Text = "Cambio: $0.00";
            // 
            // txtImporteRecibido
            // 
            txtImporteRecibido.Location = new Point(20, 192);
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
            cboFormaPago.Location = new Point(218, 122);
            cboFormaPago.Name = "cboFormaPago";
            cboFormaPago.Size = new Size(192, 23);
            cboFormaPago.TabIndex = 7;
            // 
            // cboMetodoPago
            // 
            cboMetodoPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMetodoPago.FormattingEnabled = true;
            cboMetodoPago.Location = new Point(20, 122);
            cboMetodoPago.Name = "cboMetodoPago";
            cboMetodoPago.Size = new Size(192, 23);
            cboMetodoPago.TabIndex = 6;
            // 
            // lblResumenCobro
            // 
            lblResumenCobro.AutoSize = true;
            lblResumenCobro.Location = new Point(20, 241);
            lblResumenCobro.Name = "lblResumenCobro";
            lblResumenCobro.Size = new Size(38, 15);
            lblResumenCobro.TabIndex = 5;
            lblResumenCobro.Text = "label1";
            // 
            // btnConfirmarCobro
            // 
            btnConfirmarCobro.Location = new Point(655, 354);
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
            cboUsoCFDI.Location = new Point(416, 58);
            cboUsoCFDI.Name = "cboUsoCFDI";
            cboUsoCFDI.Size = new Size(192, 23);
            cboUsoCFDI.TabIndex = 3;
            // 
            // txtRazon
            // 
            txtRazon.Enabled = false;
            txtRazon.Location = new Point(218, 58);
            txtRazon.Name = "txtRazon";
            txtRazon.Size = new Size(192, 23);
            txtRazon.TabIndex = 2;
            // 
            // txtRFC
            // 
            txtRFC.CharacterCasing = CharacterCasing.Upper;
            txtRFC.Enabled = false;
            txtRFC.Location = new Point(20, 58);
            txtRFC.Name = "txtRFC";
            txtRFC.Size = new Size(192, 23);
            txtRFC.TabIndex = 1;
            // 
            // chkFacturarAhora
            // 
            chkFacturarAhora.AutoSize = true;
            chkFacturarAhora.Location = new Point(20, 20);
            chkFacturarAhora.Name = "chkFacturarAhora";
            chkFacturarAhora.Size = new Size(102, 19);
            chkFacturarAhora.TabIndex = 0;
            chkFacturarAhora.Text = "Facturar ahora";
            chkFacturarAhora.UseVisualStyleBackColor = true;
            // 
            // tabInventario
            // 
            tabInventario.Controls.Add(btnInvAplicarCostoTodos);
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
            tabInventario.Size = new Size(792, 422);
            tabInventario.TabIndex = 4;
            tabInventario.Text = "tabInventario";
            tabInventario.UseVisualStyleBackColor = true;
            // 
            // btnInvAplicarCostoTodos
            // 
            btnInvAplicarCostoTodos.Location = new Point(678, 85);
            btnInvAplicarCostoTodos.Name = "btnInvAplicarCostoTodos";
            btnInvAplicarCostoTodos.Size = new Size(96, 23);
            btnInvAplicarCostoTodos.TabIndex = 13;
            btnInvAplicarCostoTodos.Text = "Costo a todos";
            btnInvAplicarCostoTodos.UseVisualStyleBackColor = true;
            // 
            // btnInvLimpiar
            // 
            btnInvLimpiar.Location = new Point(506, 173);
            btnInvLimpiar.Name = "btnInvLimpiar";
            btnInvLimpiar.Size = new Size(75, 23);
            btnInvLimpiar.TabIndex = 12;
            btnInvLimpiar.Text = "Limpiar";
            btnInvLimpiar.UseVisualStyleBackColor = true;
            // 
            // btnInvEliminar
            // 
            btnInvEliminar.Location = new Point(390, 173);
            btnInvEliminar.Name = "btnInvEliminar";
            btnInvEliminar.Size = new Size(91, 23);
            btnInvEliminar.TabIndex = 11;
            btnInvEliminar.Text = "Eliminar línea";
            btnInvEliminar.UseVisualStyleBackColor = true;
            btnInvEliminar.Click += btnInvEliminar_Click;
            // 
            // btnInvGuardarAux
            // 
            btnInvGuardarAux.Location = new Point(269, 173);
            btnInvGuardarAux.Name = "btnInvGuardarAux";
            btnInvGuardarAux.Size = new Size(99, 23);
            btnInvGuardarAux.TabIndex = 10;
            btnInvGuardarAux.Text = "Guardar en Aux";
            btnInvGuardarAux.UseVisualStyleBackColor = true;
            // 
            // lblInvTotales
            // 
            lblInvTotales.AutoSize = true;
            lblInvTotales.Location = new Point(360, 155);
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
            dgvInvCaptura.Location = new Point(260, 202);
            dgvInvCaptura.Name = "dgvInvCaptura";
            dgvInvCaptura.RowHeadersVisible = false;
            dgvInvCaptura.Size = new Size(532, 220);
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
            lbInvArticulos.Size = new Size(260, 422);
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
            tabConfig.Size = new Size(792, 422);
            tabConfig.TabIndex = 3;
            tabConfig.Text = "tabConfig";
            tabConfig.UseVisualStyleBackColor = true;
            // 
            // btnGuardarConfig
            // 
            btnGuardarConfig.Location = new Point(626, 335);
            btnGuardarConfig.Name = "btnGuardarConfig";
            btnGuardarConfig.Size = new Size(75, 23);
            btnGuardarConfig.TabIndex = 12;
            btnGuardarConfig.Text = "Guardar";
            btnGuardarConfig.UseVisualStyleBackColor = true;
            btnGuardarConfig.Click += btnGuardarConfig_Click;
            // 
            // btnProbarBascula
            // 
            btnProbarBascula.Location = new Point(595, 262);
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
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(tabMain);
            Name = "Form1";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
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
        private Button btnPrecuentaMesa;
        private Button btnCerrarMesa;
        private Button btnTransferirMesa;
        private Label lblMesaTiempo;
        private Button btnReabrirMesa;
        private Label lblPesoStatus;
        private CheckBox chkAutoAgregarPesables;
        private Button btnDuplicarLinea;
        private Button btnDividirLinea;
        private Button btnNotasPartida;
        private Button btnInvAplicarCostoTodos;
    }
}
