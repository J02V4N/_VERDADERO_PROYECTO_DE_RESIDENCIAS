using FirebirdSql.Data.FirebirdClient;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Text.RegularExpressions;

namespace GastroSAE  ///inicio namespace
{
    public partial class Form1 : Form   ///inicio public partial class Form1 : Form
    {

        private FbConnection? _saeConn; ///llamada a la conexion de bd
        private TextBox? txtNombreNegocio;
        private Label? lblNombreNegocio;
        private ComboBox? cboTicketAncho;
        private Label? lblTicketAncho;
        private ComboBox? cboTicketReprintNv;
        private Label? lblTicketReprintNv;
        private Button? btnTicketReprintNv;
        private Label? lblCobroProcesando;
        private ProgressBar? progressCobroProcesando;
        private TicketDocumentData? _ultimoTicketData;
        private const decimal TASA_IVA = 0.16m;

        private sealed class TicketNotaOption
        {
            public string CveDoc { get; set; } = string.Empty;
            public int Folio { get; set; }
            public decimal Total { get; set; }
            public DateTime Fecha { get; set; }
            public override string ToString()
                => $"{CveDoc.TrimStart()}  |  {Fecha:dd/MM/yyyy}  |  ${Total:N2}";
        }

        private static decimal CalcularConIva(decimal baseNeta)
            => Math.Round(baseNeta * (1m + TASA_IVA), 2);

        private static string FormatearResumenFiscal(decimal subtotal, decimal iva, decimal total)
            => $"Base: ${subtotal:N2}   IVA: ${iva:N2}   Total c/IVA: ${total:N2}";

        private static string FormatearTotalCobro(decimal total)
            => $"Total a cobrar: ${total:N2}";

        // ======== MODELOS SIMPLES (en memoria) ========

        public enum MesaEstado { LIBRE, OCUPADA, EN_CUENTA, CERRADA }

        public class Mesa
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int Capacidad { get; set; }
            public MesaEstado Estado { get; set; }
            public int? MeseroId { get; set; }


            public string MeseroNombre { get; set; }     // ← NUEVA (para mostrar en la grilla)

        }




        public class Mesero
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public class Platillo
        {
            public string Clave { get; set; }          // CLAVE_ART en SAE
            public string Nombre { get; set; }
            public decimal PrecioUnit { get; set; }    // $ por pieza o por kg (según RequierePeso)
            public bool RequierePeso { get; set; }     // true si se vende por gramos (PrecioUnit = $/kg)
            public bool Disponible { get; set; } = true;
            public bool Limitado { get; set; }
            public decimal PorcionesDisponibles { get; set; }
            public decimal PorcionesDisponiblesBase { get; set; }
            public bool DisponibleBase { get; set; } = true;
            public bool LimitadoBase { get; set; }
            public string MotivoDisponibilidad { get; set; } = string.Empty;
            public string MotivoDisponibilidadBase { get; set; } = string.Empty;

            public override string ToString()
            {
                var precioVisual = CalcularConIva(PrecioUnit);
                var precio = RequierePeso ? $"(${precioVisual:N2}/kg c/IVA)" : $"${precioVisual:N2}";
                if (!Disponible)
                {
                    var tag = (!string.IsNullOrWhiteSpace(MotivoDisponibilidad) &&
                               (MotivoDisponibilidad.Contains("insumo", StringComparison.OrdinalIgnoreCase) ||
                                MotivoDisponibilidad.Contains("receta", StringComparison.OrdinalIgnoreCase)))
                        ? "Sin insumos"
                        : "No disponible";
                    return $"{Nombre} {precio}  [{tag}]";
                }
                if (Limitado) return $"{Nombre} {precio}  [Disp.: {FormatearPorciones(PorcionesDisponibles)}]";
                return $"{Nombre} {precio}";
            }

            private static string FormatearPorciones(decimal valor)
            {
                return Math.Abs(valor - Math.Round(valor, 0)) < 0.0001m
                    ? Math.Round(valor, 0).ToString("N0")
                    : valor.ToString("N2");
            }
        }

        public class PedidoDet
        {
            public int Partida { get; set; }
            public string Clave { get; set; }
            public string Nombre { get; set; }
            public decimal Cantidad { get; set; }   // piezas/porciones si !RequierePeso; 1 si pesa
            public decimal? PesoGr { get; set; }    // gramos si RequierePeso
            public decimal PrecioUnit { get; set; } // $/pieza o $/kg

            public int? IdDet { get; set; }   // id de PEDIDO_DET en BD

            public bool RequierePeso { get; set; }
            public decimal Importe
            {
                get
                {
                    if (RequierePeso)
                    {
                        var kg = (PesoGr ?? 0m) / 1000m;   // PrecioUnit es $/kg
                        return Math.Round(kg * PrecioUnit, 2);
                    }
                    else
                    {
                        return Math.Round(Cantidad * PrecioUnit, 2);
                    }
                }
            }

            public decimal PrecioUnitConIva => CalcularConIva(PrecioUnit);
            public decimal ImporteConIva => CalcularConIva(Importe);
        }

        public class Pedido
        {
            public int Id { get; set; }
            public int MesaId { get; set; }
            public int MeseroId { get; set; }
            public BindingList<PedidoDet> Detalles { get; set; } = new BindingList<PedidoDet>();
            public bool FacturarAhora { get; set; }
            public decimal Subtotal => Math.Round(Detalles.Sum(d => d.Importe), 2);
            public decimal Impuesto => Math.Round(Subtotal * TASA_IVA, 2);
            public decimal Total => Math.Round(Subtotal + Impuesto, 2);
        }

        // ======== DATOS EN MEMORIA ========
        private BindingList<Mesa> _mesas = new BindingList<Mesa>();
        private List<Mesero> _meseros = new List<Mesero>();
        private List<Platillo> _platillos = new List<Platillo>();
        private readonly Dictionary<int, Pedido> _pedidosAbiertos = new Dictionary<int, Pedido>(); // MesaId -> Pedido

        private Mesa _mesaSeleccionada;
        private Pedido _pedidoActual;

        private System.Windows.Forms.Timer _timerBascula = new System.Windows.Forms.Timer();
        private ToolStripStatusLabel _tslAgotados;
        private ToolStripStatusLabel _tslBajos;
        private ToolStripStatusLabel _tslNoDisponibles;
        private ToolStripStatusLabel _tslLimitados;
        private ToolStripStatusLabel _tslVerAlertas;
        private List<SaeDb.InsumoAlertDto> _alertasAgotados = new();
        private List<SaeDb.InsumoAlertDto> _alertasBajos = new();
        private List<SaeDb.PlatilloDisponibilidadDto> _alertasPlatillosNoDisponibles = new();
        private List<SaeDb.PlatilloDisponibilidadDto> _alertasPlatillosLimitados = new();

        public Form1()
        {



            InitializeComponent();
            AppIcon.Apply(this);

            EnsureCobroFacturaExtraControls();
            EnsureNombreNegocioConfigControls();
            EnsureTicketWidthConfigControls();
            EnsureTicketReprintConfigControls();
            EnsureCobroProcessingControls();

            ApplyUiCopy();

            // Refactor de layout (responsive real con TableLayout/SplitContainer)
            UiRefactorMain.Apply(this);


            // Estilo (touch + accesibilidad)
            UiStyle.Apply(this);

            // Placeholders / descripciones
            UiFields.Apply(this);

            // Leyendas de atajos (shortcuts) sobre los controles clave
            UiHints.Attach(this, new Dictionary<string, string>
            {
                // Mesas
                ["dgvMesas"] = "Mesas • Enter — Abrir",
                ["btnAbrirMesa"] = "Enter",
                ["btnAsignarMesero"] = "Ctrl+M",
                ["btnLiberarMesa"] = "Ctrl+L",

                // Pedido
                ["txtBuscarPlatillo"] = "Buscar platillo • Ctrl+K",
                ["lbPlatillos"] = "Platillos • Enter — Agregar",
                ["btnQuitarLinea"] = "Del / Ctrl+D",
                ["dgvPedido"] = "Pedido • Del / Ctrl+D — Quitar",
                ["dgvReceta"] = "Receta • Solo lectura",
                ["btnIrCobro"] = "Ctrl+Enter / F5",

                // Cobro
                ["btnCobroConfirmar"] = "Enter / Ctrl+Enter",
                ["btnCobroCancelar"] = "Esc",
                ["txtCobroEfectivo"] = "Alt+E",
                ["txtCobroTarjeta"] = "Alt+T",
                ["txtCobroRef"] = "Alt+R",
                ["txtRFC"] = "RFC (CFDI)",
                ["txtRazon"] = "Razón social (CFDI)",
                ["txtCodigoPostalCfdi"] = "Código postal",
                ["cboRegFiscalCfdi"] = "Régimen fiscal",
                ["cboUsoCFDI"] = "Uso CFDI",
                ["cboMetodoPago"] = "Método de pago",
                ["cboFormaPago"] = "Forma de pago",

                // Inventario
                // txtInvBuscar usa placeholder para no encimar el botón “Refrescar”
                ["lbInvArticulos"] = "Artículos SAE • Enter — Agregar",
                ["btnInvAgregar"] = "Ins / Ctrl+N",
                ["btnInvEliminar"] = "Del / Ctrl+D",
                ["btnInvLimpiar"] = "Ctrl+L",
                ["btnInvGuardarAux"] = "Ctrl+S",
                ["btnInvRefrescar"] = "F5",
                ["chkInvSimularBascula"] = "Báscula • Ctrl+B",
                ["txtInvPesoGr"] = "Cantidad de entrada",
                ["txtInvCostoKg"] = "Costo por unidad de captura",
                ["dgvInvCaptura"] = "Captura • Del — Eliminar",

                // Config
                ["txtRutaSae"] = "Ruta SAE",
                ["txtRutaAux"] = "Ruta AUX",
                ["cboImpresora"] = "Impresora tickets",
                ["txtPuertoCom"] = "Puerto COM",
                ["cboAlmacen"] = "Almacén",
                ["cboListaPrecios"] = "Lista de precios",
                ["txtNombreNegocio"] = "Nombre del negocio (ticket)",
                ["cboTicketAncho"] = "Ancho ticket",
                ["btnCfgMesas"] = "Ctrl+Shift+M",
                ["btnCfgMeseros"] = "Ctrl+Shift+E",
                ["btnCfgRecetas"] = "Ctrl+Shift+R",
                ["btnCfgIngredientes"] = "Ctrl+Shift+I",
            });

            ResetContextoMesaPedido();
            this.Load += Form1_Load; // <-- SUSCRIBIR
            // Config inicial del timer de “báscula”
            _timerBascula.Interval = 800; // ms
            _timerBascula.Tick += (s, e) => SimularLecturaBascula();

            // IDs del contexto actual (turno/mesa/pedido) cuando una mesa está abierta


            // (opcional) helper para limpiar el contexto


            this.Shown += (s, e) => EnsureMesaSeleccionada();





                    // Maneja el fin de edición en el grid de pedido y recalcula totales
        


        }


        private void EnsureNombreNegocioConfigControls()
        {
            txtNombreNegocio ??= new TextBox
            {
                Name = "txtNombreNegocio",
                Size = new Size(320, 31),
                TabIndex = 20
            };
            lblNombreNegocio ??= new Label
            {
                Name = "lblNombreNegocio",
                Text = "Nombre del negocio",
                AutoSize = true
            };

            if (!tabConfig.Controls.Contains(txtNombreNegocio))
                tabConfig.Controls.Add(txtNombreNegocio);
            if (!tabConfig.Controls.Contains(lblNombreNegocio))
                tabConfig.Controls.Add(lblNombreNegocio);
        }

        private void EnsureTicketWidthConfigControls()
        {
            cboTicketAncho ??= new ComboBox
            {
                Name = "cboTicketAncho",
                Size = new Size(180, 33),
                DropDownStyle = ComboBoxStyle.DropDownList,
                TabIndex = 21
            };
            lblTicketAncho ??= new Label
            {
                Name = "lblTicketAncho",
                Text = "Ancho ticket",
                AutoSize = true
            };

            if (cboTicketAncho.Items.Count == 0)
            {
                cboTicketAncho.Items.AddRange(new object[] { "58", "63", "70", "80" });
                cboTicketAncho.SelectedItem = "58";
            }

            if (!tabConfig.Controls.Contains(cboTicketAncho))
                tabConfig.Controls.Add(cboTicketAncho);
            if (!tabConfig.Controls.Contains(lblTicketAncho))
                tabConfig.Controls.Add(lblTicketAncho);
        }

        private void EnsureTicketReprintConfigControls()
        {
            cboTicketReprintNv ??= new ComboBox
            {
                Name = "cboTicketReprintNv",
                Size = new Size(320, 33),
                DropDownStyle = ComboBoxStyle.DropDownList,
                TabIndex = 22
            };
            lblTicketReprintNv ??= new Label
            {
                Name = "lblTicketReprintNv",
                Text = "Reimprimir ticket de NV",
                AutoSize = true
            };
            btnTicketReprintNv ??= new Button
            {
                Name = "btnTicketReprintNv",
                Text = "Reimprimir ticket",
                Size = new Size(180, 38),
                TabIndex = 23,
                UseVisualStyleBackColor = true
            };
            btnTicketReprintNv.Click -= BtnTicketReprintNv_Click;
            btnTicketReprintNv.Click += BtnTicketReprintNv_Click;

            if (!tabConfig.Controls.Contains(cboTicketReprintNv))
                tabConfig.Controls.Add(cboTicketReprintNv);
            if (!tabConfig.Controls.Contains(lblTicketReprintNv))
                tabConfig.Controls.Add(lblTicketReprintNv);
            if (!tabConfig.Controls.Contains(btnTicketReprintNv))
                tabConfig.Controls.Add(btnTicketReprintNv);
        }

        private void BtnTicketReprintNv_Click(object? sender, EventArgs e)
        {
            ReimprimirTicketSeleccionadoConfig();
        }

        private void EnsureBusinessNameConfigured()
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            var actual = AuxDbInitializer.GetConfig(aux, "NEGOCIO_NOMBRE")?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(actual))
            {
                if (txtNombreNegocio != null) txtNombreNegocio.Text = actual;
                return;
            }

            using var f = new SimpleTextPromptForm("Nombre del negocio", "Captura el nombre del negocio para usarlo en el ticket:", string.Empty);
            while (f.ShowDialog(this) == DialogResult.OK)
            {
                var valor = (f.Value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(valor))
                {
                    MessageBox.Show(this, "Debes capturar el nombre del negocio.", "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                AuxDbInitializer.UpsertConfig(aux, "NEGOCIO_NOMBRE", valor);
                if (txtNombreNegocio != null) txtNombreNegocio.Text = valor;
                return;
            }
        }

        private void ApplyUiCopy()
        {
            try
            {
                Text = "GastroSAE";

                // Tabs
                tabMesas.Text = "Mesas";
                tabPedido.Text = "Pedido";
                tabCobro.Text = "Cobro";
                tabInventario.Text = "Inventario";
                tabConfig.Text = "Configuración";

                // Mesas
                btnAsignarMesero.Text = "Asignar mesero";
                btnLiberarMesa.Text = "Liberar mesa";

                // Pedido
                btnAgregarLinea.Text = "Agregar";
                btnQuitarLinea.Text = "Quitar";
                btnIrCobro.Text = "Ir a cobro";
                label1.Text = "Buscar platillo";

                // Cobro
                btnCobroConfirmar.Text = "Cobrar";
                btnCobroCancelar.Text = "Regresar";
                chkFacturarAhora.Text = "Facturar (CFDI)";

                // Inventario
                btnInvRefrescar.Text = "Refrescar SAE";
                btnInvAgregar.Text = "Agregar entrada";
                btnInvGuardarAux.Text = "Guardar";
                btnInvEliminar.Text = "Eliminar";
                btnInvLimpiar.Text = "Limpiar";
                chkInvSimularBascula.Text = "Simular báscula";

                // Config
                btnCfgMesas.Text = "Mesas";
                btnCfgMeseros.Text = "Meseros";
                btnCfgRecetas.Text = "Recetas";
                btnCfgIngredientes.Text = "Ingredientes";
                btnGuardarConfig.Text = "Guardar";
            }
            catch { /* no-op */ }
        }


        private int? _idTurnoActual;
        private int? _idMesaTurnoActual;
        private int? _idPedidoActual;

        private void ResetContextoMesaPedido()
        {
            _idTurnoActual = null;
            _idMesaTurnoActual = null;
            _idPedidoActual = null;
        }


        private void ConfigurarGridReceta()
        {
            dgvReceta.AutoGenerateColumns = false;
            dgvReceta.Columns.Clear();

            dgvReceta.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Clave",
                HeaderText = "Insumo",
                Width = 120,
                ReadOnly = true
            });
            dgvReceta.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Descripcion",
                HeaderText = "Descripción",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            dgvReceta.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Unidad",
                HeaderText = "UM",
                Width = 60,
                ReadOnly = true
            });
            dgvReceta.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CantidadDisplay",
                HeaderText = "Cant x 1",
                Width = 90,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            dgvReceta.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ExistenciaDisplay",
                HeaderText = "Exist.",
                Width = 90,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvReceta.AllowUserToAddRows = false;
            dgvReceta.AllowUserToDeleteRows = false;
            dgvReceta.MultiSelect = false;
            dgvReceta.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvReceta.DataSource = _recetaActual;
        }



        private void Form1_Load(object sender, EventArgs e)
        {

            lbInvArticulos.DoubleClick += (s, e) => AgregarEntradaInventario();

            AuxRepo.InicializarSesionTemporal();
            InicializarCombosConfig();
            CargarMeserosDesdeAux();
            CargarMesasDesdeAux();
            CargarPlatillosDesdeSae_ListBox();
            CargarImpresoras();
            // 0) Lee configuración persistida (impresora, almacén, lista de precios, puerto báscula)
            CargarConfigUI();
            EnsureBusinessNameConfigured();
            CargarNotasReimpresionConfig();

            // 1) Carga de catálogos 




            // 2) Bindings
            ConfigurarGrids();

            // Inventario: configura columnas y liga la sesión en memoria
            ConfigurarGridInventario();
            dgvMesas.DataSource = _mesas;
            cboMesero.DataSource = _meseros;
            cboMesero.DisplayMember = "Nombre";
            cboMesero.ValueMember = "Id";
            lbPlatillos.DataSource = _platillos;
            lbPlatillos.DrawMode = DrawMode.OwnerDrawFixed;
            lbPlatillos.DrawItem += lbPlatillos_DrawItem;

            // 3) Eventos de UI
            dgvMesas.SelectionChanged += (s, ev) => SeleccionarMesa();
            dgvMesas.SelectionChanged += (s, ev) => EnsureMesaSeleccionada();

            dgvMesas.SelectionChanged += dgvMesas_SelectionChanged;



            lbPlatillos.DoubleClick += (s, ev) => AgregarPlatilloSeleccionado();
            btnAgregarLinea.Click += (s, ev) => AgregarPlatilloSeleccionado();
            //chkSimularBascula.CheckedChanged += (s, e) => UpdateScaleTimer();

            btnIrCobro.Click += (s, ev) => IrACobro();
            
            

            // Hotkeys una sola vez:
            lbInvArticulos.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AgregarEntradaInventario(); e.Handled = true; } };
            dgvInvCaptura.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { EliminarLineaInventario(); e.Handled = true; } };

            ActualizarUI();

            this.KeyPreview = true; // para atajos




            UpdateStatus("SAE", true); // si ya probaste OK
            UpdateStatus("AUX", true);
            UpdateStatus("BAS", _timerBascula.Enabled);
            InicializarCentroAlertasUI();
            ApplyBasculaUiVisibility();


            dgvMesas.RowPrePaint += (s, e) =>
            {
                var m = dgvMesas.Rows[e.RowIndex].DataBoundItem as Mesa;
                if (m == null) return;
                var c = System.Drawing.Color.White;
                if (m.Estado == MesaEstado.OCUPADA) c = System.Drawing.Color.Moccasin;
                if (m.Estado == MesaEstado.EN_CUENTA) c = System.Drawing.Color.Khaki;
                if (m.Estado == MesaEstado.CERRADA) c = System.Drawing.Color.Gainsboro;
                dgvMesas.Rows[e.RowIndex].DefaultCellStyle.BackColor = c;
            };

            // Eventos Inventario
            txtInvBuscar.TextChanged += (s, e) => FiltrarInvArticulos();
            tabMain.SelectedIndexChanged += (s, e) =>
            {
                if (tabMain.SelectedTab == tabMesas || tabMain.SelectedTab == tabPedido || tabMain.SelectedTab == tabInventario)
                    RefrescarCentroAlertas(false);

                if (tabMain.SelectedTab == tabCobro)
                    RecargarMesasCobroSelector(_mesaSeleccionada?.Id);
                if (tabMain.SelectedTab == tabConfig)
                    CargarNotasReimpresionConfig();
            };
            btnInvRefrescar.Click += (s, e) => CargarInvArticulosDesdeSAE();
            chkInvSimularBascula.CheckedChanged += (s, e) => UpdateScaleTimer();
            btnInvAgregar.Click += (s, e) => AgregarEntradaInventario();
            btnInvGuardarAux.Click += (s, e) => GuardarEntradasInventarioEnSae();
            btnInvLimpiar.Click += (s, e) => LimpiarCapturaInventario();

            // Si NO se está simulando, permite capturar gramos manualmente.
            txtInvPesoGr.TextChanged += (s, e) => ActualizarVistaCantidadInventario();

            // Botón de pruebas (por ahora: muestra guía). La lectura real por COM se implementa después.
            btnProbarBascula.Click += (s, e) =>
                MessageBox.Show(
                    "Lectura REAL de báscula por puerto COM aún no está implementada.\n\n" +
                    "Por ahora usa la opción 'Simular báscula' en la pestaña Inventario, " +
                    "o desmárcala para capturar gramos manualmente.",
                    "Báscula",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

            

            lbPlatillos.SelectedIndexChanged += lbPlatillos_SelectedIndexChanged;


            




            
            btnCobroCancelar.Click += (s, e) => { tabMain.SelectedTab = tabPedido; };
            btnCobroConfirmar.Click += btnCobroConfirmar_Click;

            // Cobro rápido: recalcular cambio cuando cambian montos
            txtCobroEfectivo.TextChanged += (s, e) => CobroRapido_Recalcular_UI();
            txtCobroTarjeta.TextChanged += (s, e) => CobroRapido_Recalcular_UI();




            InicializarTabCobro();

            
            cboFormaPago.SelectedIndexChanged += CboFormaPago_SelectedIndexChanged;

            btnReimprimir.Click += (s, e) => ReimprimirUltimoTicket();




            // Doble-click en la lista de platillos => agregar
            //lbPlatillos.DoubleClick += (s, e) => AgregarPlatilloSeleccionado();



            // Doble-click en la grilla del pedido => quitar línea
            dgvPedido.CellDoubleClick += dgvPedido_CellDoubleClick;

            // (si agregas el botón "Quitar" en el diseñador con Name=btnQuitarLinea)
            btnQuitarLinea.Click += btnQuitarLinea_Click;


            // Carga inicial del catálogo desde SAE (si quieres al abrir)
            CargarInvArticulosDesdeSAE();

            // Alinea estado inicial de timer/lectura
            UpdateScaleTimer();


            ConfigurarGridReceta();
            RefrescarCentroAlertas(false);



        }

        private void ConfigurarGridInventario()
        {
            dgvInvCaptura.AutoGenerateColumns = false;
            dgvInvCaptura.Columns.Clear();

            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvPartida",
                HeaderText = "#",
                DataPropertyName = "Partida",
                Width = 50,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvClave",
                HeaderText = "Clave",
                DataPropertyName = "Clave",
                Width = 130,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvNombre",
                HeaderText = "Descripción",
                DataPropertyName = "Nombre",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvCantidadCaptura",
                HeaderText = "Captura",
                DataPropertyName = "CantidadCaptura",
                Width = 90,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N3",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvUnidadCaptura",
                HeaderText = "UM cap",
                DataPropertyName = "UnidadCaptura",
                Width = 75,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvCantidadBase",
                HeaderText = "Base",
                DataPropertyName = "CantidadBase",
                Width = 90,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N3",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvUnidadBase",
                HeaderText = "UM base",
                DataPropertyName = "UnidadBase",
                Width = 85,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvCostoBase",
                HeaderText = "Costo/UM",
                DataPropertyName = "CostoUnitBase",
                Width = 95,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvImporte",
                HeaderText = "Importe",
                DataPropertyName = "Importe",
                Width = 95,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            dgvInvCaptura.AllowUserToAddRows = false;
            dgvInvCaptura.AllowUserToDeleteRows = false;
            dgvInvCaptura.MultiSelect = false;
            dgvInvCaptura.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvInvCaptura.DataSource = _invEntradas;

            dgvInvCaptura.CellEndEdit -= dgvInvCaptura_CellEndEdit_Inv;
            dgvInvCaptura.CellEndEdit += dgvInvCaptura_CellEndEdit_Inv;
        }

        private void dgvInvCaptura_CellEndEdit_Inv(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvInvCaptura.Rows[e.RowIndex].DataBoundItem is EntradaInvSession it)
            {
                if (it.CantidadCaptura < 0) it.CantidadCaptura = 0;
                if (it.CostoUnitBase < 0) it.CostoUnitBase = 0;
            }
            dgvInvCaptura.Refresh();
            RecalcularTotalesInventario();
        }


        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        private void UpdateStatus(string who, bool ok)
        {
            if (who == "SAE") tslSae.Text = ok ? "SAE: Conectado" : "SAE: OFF";
            if (who == "AUX") tslAux.Text = ok ? "Aux: Conectada" : "Aux: OFF";
            if (who == "BAS")
            {
                tslBascula.Text = ok ? "Báscula: ON" : "Báscula: OFF";
                tslBascula.Visible = FeatureFlags.BasculaVisible;
            }
        }
        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        private void ApplyBasculaUiVisibility()
        {
            if (FeatureFlags.BasculaVisible) return;

            _timerBascula.Enabled = false;

            if (chkInvSimularBascula != null)
            {
                chkInvSimularBascula.Checked = false;
                chkInvSimularBascula.Visible = false;
                chkInvSimularBascula.Enabled = false;
            }

            if (txtPuertoCom != null)
            {
                txtPuertoCom.Visible = false;
                txtPuertoCom.Enabled = false;
            }

            if (btnProbarBascula != null)
            {
                btnProbarBascula.Visible = false;
                btnProbarBascula.Enabled = false;
            }

            if (tslBascula != null)
                tslBascula.Visible = false;

            UpdateStatus("BAS", false);
        }

        // ========  DATA QUE VIENE DE AUXILIAR ========



        private void CargarMeserosDesdeAux()
        {
            var lista = AuxRepo.ListarMeseros(soloActivos: true);
            _meseros = lista.ConvertAll(x => new Mesero { Id = x.Id, Nombre = x.Nombre /* puedes guardar Activo si agregas la prop */ });
            cboMesero.DataSource = null;
            cboMesero.DataSource = _meseros;
            cboMesero.DisplayMember = "Nombre";
            cboMesero.ValueMember = "Id";
        }

        private void CargarMesasDesdeAux()
        {
            _mesas.Clear();
            var lista = AuxRepo.ListarMesas();
            foreach (var m in lista)
            {
                _mesas.Add(new Mesa
                {
                    Id = m.Id,
                    Nombre = m.Nombre,
                    Capacidad = m.Capacidad ?? 0,
                    Estado = Enum.TryParse<MesaEstado>(m.Estado ?? "LIBRE", out var est) ? est : MesaEstado.LIBRE,
                    MeseroId = m.MeseroIdActual,                 // ← viene de MESA_TURNO (si hay turno abierto)
                    MeseroNombre = m.MeseroNombre                // ← para mostrar en la columna "Mesero"
                });
            }
            dgvMesas.DataSource = null;
            dgvMesas.DataSource = _mesas;
            


            // Ajusta habilitación de controles según la mesa seleccionada
            ActualizarHabilitacionMeseroSegunMesa();

            EnsureMesaSeleccionada();
        }



        private void CargarPlatillosDesdeSae_ListBox()
        {
            try
            {
                // toma lista/almacén desde CONFIG si ya los cargas a los combos; si no, defaults 1/1
                int lista = 1;
                if (int.TryParse(cboListaPrecios.Text, out var lp)) lista = lp;

                int? alm = 1;
                if (int.TryParse(cboAlmacen.Text, out var a)) alm = a;

                var rows = SaeDb.ListarDisponibilidadPlatillos(listaPrecio: lista, almacen: alm);

                // Mapea a tu clase de pedido (la ListBox usa el .ToString() que ya definiste)
                _platillos = rows.Select(r => new Platillo
                {
                    Clave = r.Clave,
                    Nombre = r.Descripcion,
                    PrecioUnit = r.Precio,       // $ por pieza (si luego quieres manejar $/kg, aquí lo adaptamos)
                    RequierePeso = false,          // por ahora en false; luego lo atamos a una bandera/INSUMO_EXT
                    Disponible = r.Disponible,
                    DisponibleBase = r.Disponible,
                    Limitado = r.Limitado,
                    LimitadoBase = r.Limitado,
                    PorcionesDisponibles = r.PorcionesPosibles,
                    PorcionesDisponiblesBase = r.PorcionesPosibles,
                    MotivoDisponibilidad = r.Motivo,
                    MotivoDisponibilidadBase = r.Motivo
                }).ToList();

                _alertasPlatillosNoDisponibles = rows.Where(x => !x.Disponible).ToList();
                _alertasPlatillosLimitados = rows.Where(x => x.Limitado).ToList();
                RecalcularDisponibilidadPlatillosEnPantalla();
                lbPlatillos.DataSource = null;
                lbPlatillos.DataSource = _platillos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible leer platillos de SAE.\n" + ex.Message,
                    "Platillos", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // fallback (opcional): deja vacía o llama SeedPlatillos();
                _platillos = new List<Platillo>();
                lbPlatillos.DataSource = _platillos;
            }
        }



        public void RefrescarCatalogosEnPantalla(string? cveArtActualizado = null, decimal? nuevoPrecioPublico = null)
        {
            string? clavePlatilloSel = (lbPlatillos.SelectedItem as Platillo)?.Clave;
            string? claveInvSel = (lbInvArticulos.SelectedItem as InvArticulo)?.Clave;

            CargarPlatillosDesdeSae_ListBox();
            if (!string.IsNullOrWhiteSpace(cveArtActualizado))
                clavePlatilloSel = cveArtActualizado;
            ReseleccionarPlatilloEnLista(clavePlatilloSel);

            CargarInvArticulosDesdeSAE();
            ReseleccionarIngredienteEnLista(claveInvSel);

            MostrarRecetaPlatilloSeleccionado();

            if (_idPedidoActual != null && !string.IsNullOrWhiteSpace(cveArtActualizado))
            {
                decimal precio = nuevoPrecioPublico ?? _platillos.FirstOrDefault(x => x.Clave == cveArtActualizado)?.PrecioUnit ?? 0m;
                if (precio > 0m)
                    AuxRepo.ActualizarPrecioUnitArticuloEnPedido(_idPedidoActual.Value, cveArtActualizado, precio);
                CargarPedidoDesdeDb();
            }

            RefrescarCentroAlertas(false);
        }

        private int ObtenerListaPrecioActual()
        {
            return int.TryParse(cboListaPrecios.Text, out var lp) ? lp : 1;
        }

        private int? ObtenerAlmacenActual()
        {
            return int.TryParse(cboAlmacen.Text, out var a) ? a : 1;
        }

        private void RefrescarCentroAlertas(bool recargarPlatillos = true)
        {
            try
            {
                string? clavePlatilloSel = (lbPlatillos.SelectedItem as Platillo)?.Clave;
                int lista = ObtenerListaPrecioActual();
                int? alm = ObtenerAlmacenActual();

                if (recargarPlatillos)
                {
                    CargarPlatillosDesdeSae_ListBox();
                }
                else
                {
                    var disp = SaeDb.ListarDisponibilidadPlatillos(listaPrecio: lista, almacen: alm);
                    var map = disp.ToDictionary(x => x.Clave, x => x, StringComparer.OrdinalIgnoreCase);
                    foreach (var p in _platillos)
                    {
                        if (!map.TryGetValue(p.Clave, out var d)) continue;
                        p.PrecioUnit = d.Precio;
                        p.DisponibleBase = d.Disponible;
                        p.LimitadoBase = d.Limitado;
                        p.PorcionesDisponiblesBase = d.PorcionesPosibles;
                        p.MotivoDisponibilidadBase = d.Motivo;
                    }
                    _alertasPlatillosNoDisponibles = disp.Where(x => !x.Disponible).ToList();
                    _alertasPlatillosLimitados = disp.Where(x => x.Limitado).ToList();
                    RecalcularDisponibilidadPlatillosEnPantalla();
                    lbPlatillos.DataSource = null;
                    lbPlatillos.DataSource = _platillos;
                }

                var alertas = SaeDb.ListarAlertasInsumos(alm);
                _alertasAgotados = alertas.Where(x => x.Tipo == "Agotado").ToList();
                _alertasBajos = alertas.Where(x => x.Tipo == "Bajo mínimo").ToList();
                ActualizarResumenAlertasUI();
                ReseleccionarPlatilloEnLista(clavePlatilloSel);
            }
            catch
            {
                // no bloquear la operación principal por fallas de alertas
            }
        }

        private void InicializarCentroAlertasUI()
        {
            _tslAgotados = new ToolStripStatusLabel("Agotados: 0");
            _tslBajos = new ToolStripStatusLabel("Bajo mínimo: 0");
            _tslNoDisponibles = new ToolStripStatusLabel("No disponibles: 0");
            _tslLimitados = new ToolStripStatusLabel("Limitados: 0");
            _tslVerAlertas = new ToolStripStatusLabel("Ver alertas") { IsLink = true };
            _tslVerAlertas.Click += (s, e) => AbrirCentroAlertas();
            statusMain.Items.Add(new ToolStripStatusLabel(" | "));
            statusMain.Items.Add(_tslAgotados);
            statusMain.Items.Add(new ToolStripStatusLabel(" | "));
            statusMain.Items.Add(_tslBajos);
            statusMain.Items.Add(new ToolStripStatusLabel(" | "));
            statusMain.Items.Add(_tslNoDisponibles);
            statusMain.Items.Add(new ToolStripStatusLabel(" | "));
            statusMain.Items.Add(_tslLimitados);
            statusMain.Items.Add(new ToolStripStatusLabel { Spring = true });
            statusMain.Items.Add(_tslVerAlertas);
        }

        private void ActualizarResumenAlertasUI()
        {
            if (_tslAgotados == null) return;
            _tslAgotados.Text = $"Agotados: {_alertasAgotados.Count}";
            _tslBajos.Text = $"Bajo mínimo: {_alertasBajos.Count}";
            _tslNoDisponibles.Text = $"No disponibles: {_alertasPlatillosNoDisponibles.Count}";
            _tslLimitados.Text = $"Limitados: {_alertasPlatillosLimitados.Count}";
            _tslAgotados.ForeColor = _alertasAgotados.Count > 0 ? Color.Firebrick : Color.DarkGreen;
            _tslBajos.ForeColor = _alertasBajos.Count > 0 ? Color.DarkOrange : Color.DarkGreen;
            _tslNoDisponibles.ForeColor = _alertasPlatillosNoDisponibles.Count > 0 ? Color.Firebrick : Color.DarkGreen;
            _tslLimitados.ForeColor = _alertasPlatillosLimitados.Count > 0 ? Color.DarkOrange : Color.DarkGreen;
        }

        private void AbrirCentroAlertas()
        {
            using var f = new FormAlertas(_alertasAgotados, _alertasBajos, _alertasPlatillosNoDisponibles, _alertasPlatillosLimitados);
            f.ShowDialog(this);
        }

        private void lbPlatillos_DrawItem(object? sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= lbPlatillos.Items.Count)
                return;

            var item = lbPlatillos.Items[e.Index];
            var platillo = item as Platillo;
            var texto = item?.ToString() ?? string.Empty;
            Color color = e.ForeColor;
            if (platillo != null)
            {
                if (!platillo.Disponible) color = Color.Firebrick;
                else if (platillo.Limitado) color = Color.DarkOrange;
            }

            TextRenderer.DrawText(e.Graphics, texto, e.Font, e.Bounds, color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            e.DrawFocusRectangle();
        }

        private void ReseleccionarPlatilloEnLista(string? clave)
        {
            if (string.IsNullOrWhiteSpace(clave) || lbPlatillos.Items.Count == 0) return;
            for (int i = 0; i < lbPlatillos.Items.Count; i++)
            {
                if (lbPlatillos.Items[i] is Platillo p && string.Equals(p.Clave, clave, StringComparison.OrdinalIgnoreCase))
                {
                    lbPlatillos.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ReseleccionarIngredienteEnLista(string? clave)
        {
            if (string.IsNullOrWhiteSpace(clave) || lbInvArticulos.Items.Count == 0) return;
            for (int i = 0; i < lbInvArticulos.Items.Count; i++)
            {
                if (lbInvArticulos.Items[i] is InvArticulo a && string.Equals(a.Clave, clave, StringComparison.OrdinalIgnoreCase))
                {
                    lbInvArticulos.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ActualizarHabilitacionMeseroSegunMesa()
        {
            var mesa = MesaSeleccionada();
            bool hayMesa = mesa != null;
            bool libre = hayMesa && mesa.Estado == MesaEstado.LIBRE;

            btnAsignarMesero.Enabled = libre;
            cboMesero.Enabled = libre;

            // Liberar solo tiene sentido si NO está libre
            btnLiberarMesa.Enabled = hayMesa && !libre;
        }


        private void dgvMesas_SelectionChanged(object sender, EventArgs e)
        {
            ActualizarHabilitacionMeseroSegunMesa();
        }


        // ======== UI / BINDINGS ========

        private void ConfigurarGrids()
        {
            // ===== MESAS =====
            dgvMesas.AutoGenerateColumns = false;
            dgvMesas.Columns.Clear();

            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMesaId",
                HeaderText = "Id",
                DataPropertyName = "Id",
                Width = 40,
                ReadOnly = true
            });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMesaNombre",
                HeaderText = "Mesa",
                DataPropertyName = "Nombre",
                Width = 100,
                ReadOnly = true
            });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMesaCap",
                HeaderText = "Cap.",
                DataPropertyName = "Capacidad",
                Width = 50,
                ReadOnly = true
            });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMesaEstado",
                HeaderText = "Estado",
                DataPropertyName = "Estado",
                Width = 90,
                ReadOnly = true
            });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMesaMesero",
                HeaderText = "Mesero",
                DataPropertyName = "MeseroNombre",   // ← usar el nombre, no el Id
                Width = 110,
                ReadOnly = true
            });

            

            // ===== PEDIDO =====
            dgvPedido.AutoGenerateColumns = false;
            dgvPedido.Columns.Clear();

            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPartida",
                HeaderText = "#",
                DataPropertyName = "Partida",
                Width = 40,
                ReadOnly = true
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colClave",
                HeaderText = "Clave",
                DataPropertyName = "Clave",
                Width = 80,
                ReadOnly = true
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNombre",
                HeaderText = "Descripción",
                DataPropertyName = "Nombre",
                Width = 190,
                ReadOnly = true
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCantidad",
                HeaderText = "Cant",
                DataPropertyName = "Cantidad",
                Width = 60,
                ReadOnly = false
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPesoGr",
                HeaderText = "Peso (g)",
                DataPropertyName = "PesoGr",
                Width = 80,
                ReadOnly = false
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrecioUnit",
                HeaderText = "P.Unit c/IVA",
                DataPropertyName = "PrecioUnitConIva",
                Width = 70,
                ReadOnly = true
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colImporte",
                HeaderText = "Importe c/IVA",
                DataPropertyName = "ImporteConIva",
                Width = 80,
                ReadOnly = true
            });

            // Formatos (usan Name, ya no HeaderText)
            dgvPedido.Columns["colPrecioUnit"].DefaultCellStyle.Format = "N2";
            dgvPedido.Columns["colImporte"].DefaultCellStyle.Format = "N2";
            dgvPedido.Columns["colPesoGr"].DefaultCellStyle.Format = "N0";
            dgvPedido.Columns["colCantidad"].DefaultCellStyle.Format = "N2";

            dgvPedido.CellEndEdit -= dgvPedido_CellEndEdit;
            dgvPedido.CellEndEdit += dgvPedido_CellEndEdit;


            // ===== INVENTARIO (captura por báscula) =====
            dgvInvCaptura.AutoGenerateColumns = false;
            dgvInvCaptura.Columns.Clear();

            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvPartida",
                HeaderText = "#",
                DataPropertyName = "Partida",
                Width = 40,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvClave",
                HeaderText = "Clave",
                DataPropertyName = "Clave",
                Width = 100,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvNombre",
                HeaderText = "Descripción",
                DataPropertyName = "Nombre",
                Width = 220,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvPesoGr",
                HeaderText = "Peso (g)",
                DataPropertyName = "PesoGr",
                Width = 80,
                ReadOnly = false
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvKg",
                HeaderText = "Kg",
                DataPropertyName = "PesoKg",
                Width = 70,
                ReadOnly = true
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvCostoKg",
                HeaderText = "Costo/Kg",
                DataPropertyName = "CostoKg",
                Width = 80,
                ReadOnly = false
            });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colInvImporte",
                HeaderText = "Importe",
                DataPropertyName = "Importe",
                Width = 80,
                ReadOnly = true
            });

            dgvInvCaptura.Columns["colInvKg"].DefaultCellStyle.Format = "N3";
            dgvInvCaptura.Columns["colInvCostoKg"].DefaultCellStyle.Format = "N2";
            dgvInvCaptura.Columns["colInvImporte"].DefaultCellStyle.Format = "N2";

            // Si quieres validar numéricos:
            dgvInvCaptura.CellValidating -= DgvInvCaptura_CellValidating;
            dgvInvCaptura.CellValidating += DgvInvCaptura_CellValidating;
        }

        

        private void DgvInvCaptura_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var name = dgvInvCaptura.Columns[e.ColumnIndex].Name;
            if (name == "colInvPesoGr" || name == "colInvCostoKg")
            {
                if (e.FormattedValue != null && !string.IsNullOrWhiteSpace(e.FormattedValue.ToString()))
                {
                    if (!decimal.TryParse(e.FormattedValue.ToString(), out var v) || v < 0)
                    { e.Cancel = true; MessageBox.Show("Valor inválido."); }
                }
            }
        }


        void SeleccionarMesa()
        {
            if (dgvMesas.CurrentRow?.DataBoundItem is Mesa m)
            {
                _mesaSeleccionada = m;
                lblMesaSel.Text = $"Seleccionada: {m.Nombre} ({m.Estado})";

                if (m.Estado == MesaEstado.LIBRE)
                {
                    _idMesaTurnoActual = null;
                    _idPedidoActual = null;
                    LimpiarPedidoActualEnPantalla();
                    RefrescarCentroAlertas(false);
                    return;
                }

                // Mesa no libre: intenta localizar el pedido abierto de HOY
                var (idPed, idMT) = AuxRepo.ObtenerPedidoAbiertoPorMesa(m.Id);
                _idMesaTurnoActual = idMT;
                _idPedidoActual = idPed;

                if (_idPedidoActual != null)
                    CargarPedidoDesdeDb();
                else
                {
                    LimpiarPedidoActualEnPantalla();
                    RefrescarCentroAlertas(false);
                }
            }
        }


        private void AbrirAtenderMesa()
        {
            EnsureMesaSeleccionada();
            var mSel = MesaSeleccionada();  // ← renombrado
            if (mSel == null)
            {
                MessageBox.Show("Selecciona una mesa para abrir.", "Mesas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (cboMesero.SelectedItem is not Mesero mesero)
            {
                MessageBox.Show("Selecciona un mesero antes de abrir la mesa.", "Mesas",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var r = AuxRepo.AbrirMesa(mSel.Id, mesero.Id);

                _idTurnoActual = r.IdTurno;
                _idMesaTurnoActual = r.IdMesaTurno;
                _idPedidoActual = r.IdPedido;

                // Actualiza UI
                mSel.Estado = MesaEstado.OCUPADA;
                RefrescarFilaMesaActual();
                mSel.MeseroId = mesero.Id;
                mSel.MeseroNombre = mesero.Nombre;
                lblMesaSel.Text = $"Seleccionada: {mSel.Nombre} ({mSel.Estado})";
                dgvMesas.Refresh();
                ActualizarHabilitacionMeseroSegunMesa();

                // Ir directo a tabPedido listo para capturar
                tabMain.SelectedTab = tabPedido;
                txtBuscarPlatillo.Focus();
                txtBuscarPlatillo.SelectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir la mesa.\n" + ex.Message,
                                "Abrir mesa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        void AgregarPlatilloSeleccionado()
        {
            if (_mesaSeleccionada == null || _idPedidoActual == null)
            {
                MessageBox.Show("No hay pedido abierto. Abre la mesa primero.", "Pedido",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (lbPlatillos.SelectedItem is not Platillo p)
            {
                MessageBox.Show("Selecciona un platillo.", "Pedido",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!p.Disponible)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(p.MotivoDisponibilidad)
                        ? "Este platillo no está disponible por falta de insumos."
                        : p.MotivoDisponibilidad,
                    "Platillo no disponible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var porcionesRestantes = ObtenerPorcionesRestantesParaPedidoActual(p);
            if (!p.RequierePeso && porcionesRestantes < 1m)
            {
                MessageBox.Show(
                    $"Ya no puedes agregar más '{p.Nombre}' a este pedido.\n\n" +
                    $"Ya pediste: {FormatearPorcionesPedido(ObtenerCantidadPedidaActualDePlatillo(p.Clave))} porciones.",
                    "Límite alcanzado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Sin peso ni báscula: una pieza por clic
            decimal cantidad = 1m;
            decimal? pesoGr = null;

            try
            {
                AuxRepo.AgregarPedidoLinea(_idPedidoActual.Value,
                                           p.Clave,
                                           esPlatillo: true,
                                           cantidad: cantidad,
                                           pesoGr: pesoGr,
                                           precioUnit: p.PrecioUnit);

                AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value, TASA_IVA);
                CargarPedidoDesdeDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo agregar la línea",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void RecalcularTotales()
        {
            // Normaliza celdas editables (Cant / Peso) y recalcula
            dgvPedido.EndEdit();
            if (_idPedidoActual != null)
            {
                var (sub, imp, tot) = AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value, TASA_IVA);
                lblTotales.Text = FormatearResumenFiscal(sub, imp, tot);
            }
            ActualizarTotales();
        }

        private void ActualizarTotales()
        {
            if (_pedidoActual == null)
            {
                lblTotales.Text = FormatearResumenFiscal(0m, 0m, 0m);
                return;
            }
            lblTotales.Text = FormatearResumenFiscal(_pedidoActual.Subtotal, _pedidoActual.Impuesto, _pedidoActual.Total);
        }

        private void IrACobro()
        {
            RecargarMesasCobroSelector(_mesaSeleccionada?.Id);

            if (_idPedidoActual == null && _cboMesaCobroSelector != null && _cboMesaCobroSelector.Items.Count > 0)
            {
                _cboMesaCobroSelector.SelectedIndex = 0;
            }

            if (_idPedidoActual == null)
            {
                MessageBox.Show("No hay mesas con pedido abierto para cobrar.", "Cobro",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PrepararPantallaCobroDesdePedidoActual();
            tabMain.SelectedTab = tabCobro;
            txtCobroEfectivo.Focus();
            txtCobroEfectivo.SelectAll();
        }



        


        [Obsolete("Usar siempre btnConfirmarCobro_Click (flujo SAE + CFDI).")]
        private void ConfirmarCobro()
        {
            if (_pedidoActual == null) return;

            _pedidoActual.FacturarAhora = chkFacturarAhora.Checked;
            // Aquí solo simulamos “cobrado”
            MessageBox.Show(_pedidoActual.FacturarAhora
                ? "Cobro confirmado. (Simulación) Facturar ahora."
                : "Cobro confirmado. (Simulación) Ticket / Facturar después.",
                "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Cerrar pedido y mesa
            _mesaSeleccionada.Estado = MesaEstado.CERRADA;
            _pedidosAbiertos.Remove(_mesaSeleccionada.Id);
            _pedidoActual = null;
            dgvPedido.DataSource = null;
            lblTotales.Text = FormatearResumenFiscal(0m, 0m, 0m);

            tabMain.SelectedTab = tabMesas;
            ActualizarUI();
        }

        private void ActualizarUI()
        {
            dgvMesas.Refresh();
            lblMesaSel.Text = _mesaSeleccionada == null
                ? "Sin mesa seleccionada"
                : $"Seleccionada: {_mesaSeleccionada.Nombre} ({_mesaSeleccionada.Estado})";
        }

        // ======== “BÁSCULA” SIMULADA ========

        private readonly Random _rnd = new Random();
        private readonly Queue<decimal> _ultLecturas = new Queue<decimal>(5);

        private void SimularLecturaBascula()
        {
            // Simulación (80–350 g)
            decimal gramos = _rnd.Next(80, 351);

            void push(decimal v)
            {
                if (_ultLecturas.Count == 5) _ultLecturas.Dequeue();
                _ultLecturas.Enqueue(v);
            }

            // Pedido
            //if (chkSimularBascula.Checked && txtPesoGr != null && !txtPesoGr.IsDisposed)
            //{
            //  txtPesoGr.Text = gramos.ToString("0");
            //push(gramos);
            //if (EsLecturaEstable() && lbPlatillos.SelectedItem is Platillo p && p.RequierePeso)
            //{
            //  AgregarPlatilloSeleccionado(); // auto-agrega con el peso estable actual
            // _ultLecturas.Clear();
            //}
            // }

            // Inventario
            if (chkInvSimularBascula.Checked && txtInvPesoGr != null && !txtInvPesoGr.IsDisposed)
            {
                txtInvPesoGr.Text = gramos.ToString("0");
                ActualizarVistaCantidadInventario();
                push(gramos);
                // Aquí no auto-agregamos, requerimos clic o Enter
            }
        }

        private bool EsLecturaEstable()
        {
            if (_ultLecturas.Count < 5) return false;
            var arr = _ultLecturas.ToArray();
            decimal prom = arr.Average();
            decimal var = arr.Select(x => (x - prom) * (x - prom)).Average();
            // umbral: desviación std <= 2g
            return Math.Sqrt((double)var) <= 2.0;
        }


        private void prueba_conexion_Click(object sender, EventArgs e)
        {

        }


        private void btnPruebaAux_Click(object sender, EventArgs e)
        {
            // 2) BD Auxiliar (crear/actualizar CONFIG)
            try
            {
                // Crea/abre la BD auxiliar en la raíz del proyecto (o junto al .exe publicado)
                string auxPath;
                using var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");

                // Prueba de vida
                using (var cmd = new FbCommand("SELECT 1 FROM RDB$DATABASE", auxConn))
                {
                    var ping = cmd.ExecuteScalar();
                    if (Convert.ToInt32(ping) != 1)
                        throw new Exception("Ping falló en BD auxiliar.");
                }

                // Cuenta tablas de usuario (no del sistema)
                int tablasUsuario;
                using (var cmd = new FbCommand(
                    @"SELECT COUNT(*) 
              FROM RDB$RELATIONS 
              WHERE RDB$SYSTEM_FLAG = 0", auxConn))
                {
                    tablasUsuario = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Verifica CONFIG y conteos del esquema slim
                int cfgCount, mesasCount, meserosCount;
                using (var cmd = new FbCommand("SELECT COUNT(*) FROM CONFIG", auxConn))
                    cfgCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new FbCommand("SELECT COUNT(*) FROM MESAS", auxConn))
                    mesasCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new FbCommand("SELECT COUNT(*) FROM MESEROS", auxConn))
                    meserosCount = Convert.ToInt32(cmd.ExecuteScalar());

                MessageBox.Show(
                    "BD Auxiliar OK\n" +
                    $"Ruta: {auxPath}\n" +
                    $"Tablas usuario: {tablasUsuario}\n" +
                    $"CONFIG rows: {cfgCount}\n" +
                    $"Mesas: {mesasCount}\n" +
                    $"Meseros: {meserosCount}",
                    "Auxiliar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information
                );
                txtRutaAux.Text = auxPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error BD Auxiliar:\n" + ex.Message, "Auxiliar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }

        }

        private void btnPruebaSae_Click(object sender, EventArgs e)
        {
            // 1) Conexión/prueba SAE
            try
            {


                // Detecta la BD de SAE (Empresa 01 por defecto)
                string saePath = GetSaePathFromAuxConfig();
                using var saeConn = SaeDb.CreateConnection(
                    databasePath: saePath,
                    server: "127.0.0.1",
                    port: 3050,
                    user: "SYSDBA",
                    password: "masterkey",
                    charset: "ISO8859_1");
                SaeDb.TestConnection(saeConn);
                txtRutaSae.Text = saePath;




                // Ping + prueba mínima INVE01 (si existe)
                SaeDb.TestConnection(saeConn);

                MessageBox.Show($"Conexión SAE OK.\nFDB: {saePath}", "SAE 9",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtRutaSae.Text = saePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error SAE 9:\n" + ex.Message, "SAE 9",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        public class InvArticulo
        {
            public string Clave { get; set; }
            public string Descr { get; set; }
            public string UniMed { get; set; }
            public string UniAlt { get; set; }
            public decimal? FacConv { get; set; }
            public decimal CostoProm { get; set; }
            public string UnidadBase => string.IsNullOrWhiteSpace(UniAlt) ? "pz" : UniAlt.Trim().ToLowerInvariant();
            public string UnidadCaptura
            {
                get
                {
                    var entrada = (UniMed ?? string.Empty).Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(entrada)) return entrada;
                    return UnidadBase;
                }
            }
            public decimal FactorConversion => FacConv.GetValueOrDefault() <= 0 ? 1m : FacConv.Value;
            public bool UsaBascula => false;
            public override string ToString() => $"{Clave} - {Descr}";
        }

        public class EntradaInvSession
        {
            public int Partida { get; set; }
            public string Clave { get; set; }
            public string Nombre { get; set; }
            public string UnidadCaptura { get; set; } = "pz";
            public string UnidadBase { get; set; } = "pz";
            public decimal FactorConversion { get; set; } = 1m;
            public decimal CantidadCaptura { get; set; }
            public decimal CantidadBase => decimal.Round(SaeCatalogAdmin.ConvertCaptureQtyToBase(UnidadBase, UnidadCaptura, FactorConversion, CantidadCaptura), 3, MidpointRounding.AwayFromZero);
            public decimal CostoUnitBase { get; set; }
            public decimal Importe => Math.Round(CantidadBase * CostoUnitBase, 2);

            // Compatibilidad con el código/Binding ya existente
            public decimal PesoGr { get => CantidadCaptura; set => CantidadCaptura = value; }
            public decimal PesoKg => CantidadBase;
            public decimal CostoKg { get => CostoUnitBase; set => CostoUnitBase = value; }
        }

        private List<InvArticulo> _invArticulos = new List<InvArticulo>();
        private BindingList<EntradaInvSession> _invEntradas = new BindingList<EntradaInvSession>();


        private void CargarInvArticulosDesdeSAE()
        {
            try
            {
                string saePath = GetSaePathFromAuxConfig();
                using var sae = SaeDb.CreateConnection(
                    saePath, server: "127.0.0.1", port: 3050,
                    user: "SYSDBA", password: "masterkey", charset: "ISO8859_1");
                sae.Open();
                // ... SELECT a INVE01 como ya lo tienes




                var invTable = SaeDb.GetTableName(sae, "INVE");
                using var cmd = new FbCommand($@"
SELECT FIRST 500
       TRIM(CVE_ART) AS CVE_ART,
       TRIM(DESCR) AS DESCR,
       TRIM(UNI_MED) AS UNI_MED,
       TRIM(UNI_ALT) AS UNI_ALT,
       COALESCE(FAC_CONV, 1) AS FAC_CONV,
       COALESCE(COSTO_PROM, 0) AS COSTO_PROM
FROM {invTable}
WHERE COALESCE(STATUS, 'A') <> 'B'
  AND COALESCE(TRIM(LIN_PROD), '') = 'Insum'
ORDER BY TRIM(CVE_ART)", sae);

                using var rd = cmd.ExecuteReader();
                var list = new List<InvArticulo>();
                int ordCve = rd.GetOrdinal("CVE_ART");
                int ordDescr = rd.GetOrdinal("DESCR");
                int ordUniMed = rd.GetOrdinal("UNI_MED");
                int ordUniAlt = rd.GetOrdinal("UNI_ALT");
                int ordFacConv = rd.GetOrdinal("FAC_CONV");
                int ordCosto = rd.GetOrdinal("COSTO_PROM");
                while (rd.Read())
                {
                    list.Add(new InvArticulo
                    {
                        Clave = rd.IsDBNull(ordCve) ? string.Empty : rd.GetString(ordCve).Trim(),
                        Descr = rd.IsDBNull(ordDescr) ? string.Empty : rd.GetString(ordDescr).Trim(),
                        UniMed = rd.IsDBNull(ordUniMed) ? string.Empty : rd.GetString(ordUniMed).Trim(),
                        UniAlt = rd.IsDBNull(ordUniAlt) ? string.Empty : rd.GetString(ordUniAlt).Trim(),
                        FacConv = rd.IsDBNull(ordFacConv) ? 1m : Convert.ToDecimal(rd.GetValue(ordFacConv)),
                        CostoProm = rd.IsDBNull(ordCosto) ? 0m : Convert.ToDecimal(rd.GetValue(ordCosto))
                    });
                }
                _invArticulos = list;
                lbInvArticulos.DataSource = null;
                lbInvArticulos.DataSource = _invArticulos;
                if (_invArticulos.Count > 0)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (lbInvArticulos.Items.Count > 0)
                            lbInvArticulos.SelectedIndex = 0;
                        ActualizarContextoInventarioSeleccionado();
                    }));
                }
                else
                {
                    ActualizarContextoInventarioSeleccionado();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible leer INVE01 de SAE.\n" + ex.Message, "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // fallback: deja la lista vacía o con dummy
                _invArticulos = new List<InvArticulo>();
                lbInvArticulos.DataSource = _invArticulos;
                ActualizarContextoInventarioSeleccionado();
            }
        }

        private void FiltrarInvArticulos()
        {
            string q = txtInvBuscar.Text.Trim().ToLowerInvariant();
            lbInvArticulos.DataSource = string.IsNullOrEmpty(q)
                ? _invArticulos
                : _invArticulos.Where(a => (a.Clave ?? "").ToLower().Contains(q)
                                        || (a.Descr ?? "").ToLower().Contains(q)).ToList();
        }

        private void AgregarEntradaInventario()
        {
            if (lbInvArticulos.SelectedItem is not InvArticulo art)
            {
                MessageBox.Show("Selecciona un ingrediente de la lista.");
                return;
            }
            if (!decimal.TryParse(txtInvPesoGr.Text, out var cantidadCaptura) || cantidadCaptura <= 0)
            {
                MessageBox.Show($"Cantidad inválida en {art.UnidadCaptura}.");
                return;
            }

            decimal costoCaptura = 0m;
            decimal.TryParse(txtInvCostoKg.Text, out costoCaptura);
            decimal costoBase = SaeCatalogAdmin.ConvertCaptureCostToBase(art.UnidadBase, art.UnidadCaptura, art.FactorConversion, costoCaptura);

            var det = new EntradaInvSession
            {
                Partida = _invEntradas.Count + 1,
                Clave = art.Clave,
                Nombre = art.Descr,
                UnidadCaptura = art.UnidadCaptura,
                UnidadBase = art.UnidadBase,
                FactorConversion = art.FactorConversion,
                CantidadCaptura = cantidadCaptura,
                CostoUnitBase = costoBase
            };
            _invEntradas.Add(det);
            dgvInvCaptura.Refresh();
            RecalcularTotalesInventario();

            if (dgvInvCaptura.Rows.Count > 0)
            {
                dgvInvCaptura.ClearSelection();
                var last = dgvInvCaptura.Rows[dgvInvCaptura.Rows.Count - 1];
                last.Selected = true;
                dgvInvCaptura.FirstDisplayedScrollingRowIndex = last.Index;
            }
        }

        private void RecalcularTotalesInventario()
        {
            dgvInvCaptura.EndEdit();
            decimal imp = _invEntradas.Sum(x => x.Importe);
            var grupos = _invEntradas
                .GroupBy(x => x.UnidadBase)
                .Select(g => $"{g.Key}: {FormatearCantidadUnidad(g.Key, g.Sum(x => x.CantidadBase))}")
                .ToList();
            var resumenCant = grupos.Count > 0 ? string.Join("   ", grupos) + "   " : string.Empty;
            lblInvTotales.Text = $"Entradas: {_invEntradas.Count}   {resumenCant}$: {imp:N2}";
        }

        private void EliminarLineaInventario()
        {
            if (dgvInvCaptura.CurrentRow?.DataBoundItem is EntradaInvSession d)
            {
                _invEntradas.Remove(d);
                int i = 1; foreach (var x in _invEntradas) x.Partida = i++;
                RecalcularTotalesInventario();
            }
        }

        private void LimpiarCapturaInventario()
        {
            _invEntradas.Clear();
            RecalcularTotalesInventario();
        }


        private void GuardarEntradasInventarioEnSae()
        {
            if (_invEntradas.Count == 0)
            {
                MessageBox.Show("No hay entradas para guardar.");
                return;
            }

            try
            {
                var items = _invEntradas
                    .Select(e => new SaeCatalogAdmin.InventarioPostItem
                    {
                        Clave = e.Clave,
                        CantidadBase = e.CantidadBase,
                        CostoUnitBase = e.CostoUnitBase,
                        UnidadBase = e.UnidadBase,
                        UnidadCaptura = e.UnidadCaptura,
                        CantidadCaptura = e.CantidadCaptura
                    })
                    .ToList();

                SaeCatalogAdmin.AplicarEntradasInventario(items);

                MessageBox.Show($"Guardadas {_invEntradas.Count} entradas directo en SAE.", "Inventario",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                _invEntradas.Clear();
                RecalcularTotalesInventario();
                CargarInvArticulosDesdeSAE();
                ActualizarVistaCantidadInventario();
                RefrescarCentroAlertas(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando en SAE:\n" + ex.Message, "Inventario",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // =========================
            // TAB: MESAS
            // =========================
            if (tabMain.SelectedTab == tabMesas)
            {
                if (keyData == Keys.Enter && dgvMesas.Focused)
                { btnAbrirMesa.PerformClick(); return true; }

                if (keyData == (Keys.Control | Keys.M))
                { btnAsignarMesero.PerformClick(); return true; }

                if (keyData == (Keys.Control | Keys.L))
                { btnLiberarMesa.PerformClick(); return true; }
            }

            // =========================
            // TAB: PEDIDO
            // =========================
            if (tabMain.SelectedTab == tabPedido)
            {
                // Cobrar rápido
                if (keyData == (Keys.Control | Keys.Enter) || keyData == Keys.F5)
                { IrACobro(); return true; }

                // Buscar platillo
                if (keyData == (Keys.Control | Keys.K))
                { txtBuscarPlatillo.Focus(); txtBuscarPlatillo.SelectAll(); return true; }

                // Volver a Mesas
                if (keyData == Keys.Escape)
                { tabMain.SelectedTab = tabMesas; return true; }

                // ENTER en la lista de platillos => agregar
                if (keyData == Keys.Enter && lbPlatillos.Focused)
                { AgregarPlatilloSeleccionado(); return true; }

                // Delete / Ctrl+D: quitar línea actual del pedido
                if ((keyData == Keys.Delete || keyData == (Keys.Control | Keys.D)) && dgvPedido.Focused)
                {
                    QuitarLineaSeleccionada();
                    return true;
                }
            }

            // =========================
            // TAB: COBRO
            // =========================
            if (tabMain.SelectedTab == tabCobro)
            {
                // Confirmar cobro
                if (keyData == Keys.Enter || keyData == (Keys.Control | Keys.Enter))
                { btnCobroConfirmar.PerformClick(); return true; }

                // Cancelar / volver
                if (keyData == Keys.Escape)
                { btnCobroCancelar.PerformClick(); return true; }

                // Ir rápido a campos
                if (keyData == (Keys.Alt | Keys.E))
                { txtCobroEfectivo.Focus(); txtCobroEfectivo.SelectAll(); return true; }

                if (keyData == (Keys.Alt | Keys.T))
                { txtCobroTarjeta.Focus(); txtCobroTarjeta.SelectAll(); return true; }

                if (keyData == (Keys.Alt | Keys.R))
                { txtCobroRef.Focus(); txtCobroRef.SelectAll(); return true; }
            }

            // =========================
            // TAB: INVENTARIO
            // =========================
            if (tabMain.SelectedTab == tabInventario)
            {
                if (keyData == (Keys.Control | Keys.F))
                { txtInvBuscar.Focus(); txtInvBuscar.SelectAll(); return true; }

                if (FeatureFlags.BasculaVisible && keyData == (Keys.Control | Keys.B))
                { chkInvSimularBascula.Checked = !chkInvSimularBascula.Checked; return true; }

                if (keyData == Keys.F5)
                { btnInvRefrescar.PerformClick(); return true; }

                if (keyData == (Keys.Control | Keys.S))
                { btnInvGuardarAux.PerformClick(); return true; }

                if (keyData == (Keys.Control | Keys.L))
                { btnInvLimpiar.PerformClick(); return true; }

                if ((keyData == Keys.Insert || keyData == (Keys.Control | Keys.N)))
                { btnInvAgregar.PerformClick(); return true; }

                if ((keyData == Keys.Delete || keyData == (Keys.Control | Keys.D)) && dgvInvCaptura.Focused)
                { EliminarLineaInventario(); return true; }

                // Salir rápido
                if (keyData == Keys.Escape)
                { tabMain.SelectedTab = tabMesas; return true; }
            }

            // =========================
            // TAB: CONFIG
            // =========================
            if (tabMain.SelectedTab == tabConfig)
            {
                if (keyData == (Keys.Control | Keys.Shift | Keys.M))
                { btnCfgMesas.PerformClick(); return true; }

                if (keyData == (Keys.Control | Keys.Shift | Keys.E))
                { btnCfgMeseros.PerformClick(); return true; }

                if (keyData == (Keys.Control | Keys.Shift | Keys.R))
                { btnCfgRecetas.PerformClick(); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.I))
                { btnCfgIngredientes.PerformClick(); return true; }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


private void btnInvEliminar_Click(object sender, EventArgs e)
        {
            EliminarLineaInventario();
        }

        private void txtBuscarPlatillo_TextChanged(object sender, EventArgs e)
        {
            string q = txtBuscarPlatillo.Text.Trim().ToLowerInvariant();
            lbPlatillos.DataSource = string.IsNullOrEmpty(q)
                ? _platillos
                : _platillos.Where(p => p.Nombre.ToLower().Contains(q) || p.Clave.ToLower().Contains(q)).ToList();

        }

        private void txtImporteRecibido_TextChanged(object sender, EventArgs e)
        {
            Cobro_RecalcularCambio_UI(sender, e);
        }


        private void CargarImpresoras()
        {
            cboImpresora.Items.Clear();
            foreach (string p in PrinterSettings.InstalledPrinters)
                cboImpresora.Items.Add(p);
        }

        private void UpdateScaleTimer()
        {
            if (!FeatureFlags.BasculaVisible)
            {
                _timerBascula.Enabled = false;
                if (chkInvSimularBascula != null) chkInvSimularBascula.Checked = false;
                if (txtInvPesoGr != null) txtInvPesoGr.ReadOnly = false;
                UpdateStatus("BAS", false);
                return;
            }

            var usaBascula = lbInvArticulos.SelectedItem is InvArticulo art && art.UsaBascula;
            if (!usaBascula && chkInvSimularBascula.Checked)
                chkInvSimularBascula.Checked = false;

            chkInvSimularBascula.Enabled = usaBascula;
            _timerBascula.Enabled = usaBascula && chkInvSimularBascula.Checked;
            txtInvPesoGr.ReadOnly = usaBascula && chkInvSimularBascula.Checked;
            UpdateStatus("BAS", usaBascula && chkInvSimularBascula.Checked);
        }


        private bool ValidarCobro()
        {
            if (_pedidoActual == null || _pedidoActual.Detalles.Count == 0) { MessageBox.Show("No hay partidas."); return false; }

            if (chkFacturarAhora.Checked)
            {
                if (!Regex.IsMatch(txtRFC.Text.Trim().ToUpperInvariant(), @"^([A-ZÑ&]{3,4})\d{6}[A-Z0-9]{3}$"))
                { MessageBox.Show("RFC inválido."); return false; }
                if (string.IsNullOrWhiteSpace(txtRazon.Text)) { MessageBox.Show("Captura Razón social."); return false; }
                if (txtCodigoPostalCfdi == null || !Regex.IsMatch((txtCodigoPostalCfdi.Text ?? string.Empty).Trim(), @"^\d{5}$")) { MessageBox.Show("Captura un Código postal válido de 5 dígitos."); return false; }
                if (cboRegFiscalCfdi == null || cboRegFiscalCfdi.SelectedIndex < 0) { MessageBox.Show("Selecciona Régimen fiscal."); return false; }
                if (string.IsNullOrWhiteSpace(cboUsoCFDI.Text)) { MessageBox.Show("Selecciona Uso CFDI."); return false; }
            }

            if (cboMetodoPago.SelectedIndex < 0) { MessageBox.Show("Selecciona método de pago."); return false; }
            if (cboMetodoPago.Text.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
            {
                if (!decimal.TryParse(txtImporteRecibido.Text, out var rec)) { MessageBox.Show("Importe recibido inválido."); return false; }
                if (rec < _pedidoActual.Total) { MessageBox.Show("Importe insuficiente."); return false; }
                var cambio = rec - _pedidoActual.Total;
                lblCambio.Text = $"Cambio: ${cambio:N2}";
            }
            return true;
        }


        private void InicializarCombosConfig()
        {
            if (cboAlmacen.Items.Count == 0)
            {
                for (int i = 1; i <= 99; i++)
                    cboAlmacen.Items.Add(i.ToString());
            }

            if (cboListaPrecios.Items.Count == 0)
            {
                for (int i = 1; i <= 20; i++)
                    cboListaPrecios.Items.Add(i.ToString());
            }
        }

        private void CargarConfigUI()
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            string Get(string k)
            {
                using var c = new FbCommand("SELECT VALOR FROM CONFIG WHERE CLAVE=@K", aux);
                c.Parameters.Add(new FbParameter("@K", FbDbType.VarChar, 50) { Value = k });
                var o = c.ExecuteScalar();
                return o == null || o == DBNull.Value ? "" : o.ToString();
            }

            txtRutaAux.Text = auxPath;

            var saePath = Get("SAE_FDB");
            if (string.IsNullOrWhiteSpace(saePath))
            {
                try
                {
                    if (Sae9Locator.TryFindSaeDatabase(1, out var autoPath, out var _))
                    {
                        saePath = autoPath;
                        AuxDbInitializer.UpsertConfig(aux, "SAE_FDB", saePath);
                    }
                }
                catch { }
            }
            txtRutaSae.Text = saePath;

            txtPuertoCom.Text = Get("BASCULA_PUERTO");
            var imp = Get("IMPRESORA_TICKET");
            if (!string.IsNullOrWhiteSpace(imp))
            {
                var match = cboImpresora.Items.Cast<object>()
                    .Select(x => x?.ToString() ?? string.Empty)
                    .FirstOrDefault(x => string.Equals(x, imp, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                    cboImpresora.SelectedItem = match;
            }

            var almacen = Get("ALMACEN_DEFAULT");
            if (!string.IsNullOrWhiteSpace(almacen) && cboAlmacen.Items.Contains(almacen))
                cboAlmacen.SelectedItem = almacen;
            else if (cboAlmacen.Items.Count > 0)
                cboAlmacen.SelectedItem = "1";

            if (txtNombreNegocio != null)
                txtNombreNegocio.Text = Get("NEGOCIO_NOMBRE");
            if (cboTicketAncho != null)
            {
                var ancho = Get("TICKET_ANCHO_MM");
                cboTicketAncho.SelectedItem = new[] { "58", "63", "70", "80" }.Contains(ancho) ? ancho : "58";
            }

            var lista = Get("LISTA_PRECIOS");
            if (!string.IsNullOrWhiteSpace(lista) && cboListaPrecios.Items.Contains(lista))
                cboListaPrecios.SelectedItem = lista;
            else if (cboListaPrecios.Items.Count > 0)
                cboListaPrecios.SelectedItem = "1";
        }

        private void EnsureCobroProcessingControls()
        {
            lblCobroProcesando ??= new Label
            {
                Name = "lblCobroProcesando",
                Text = "Procesando cobro en SAE...",
                AutoSize = true,
                Visible = false
            };
            progressCobroProcesando ??= new ProgressBar
            {
                Name = "progressCobroProcesando",
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 25,
                Size = new Size(260, 12),
                Visible = false
            };

            if (!tabCobro.Controls.Contains(lblCobroProcesando))
                tabCobro.Controls.Add(lblCobroProcesando);
            if (!tabCobro.Controls.Contains(progressCobroProcesando))
                tabCobro.Controls.Add(progressCobroProcesando);

            // Ubicación conservadora en la esquina inferior derecha del tab de cobro.
            lblCobroProcesando.BringToFront();
            progressCobroProcesando.BringToFront();
            RepositionCobroProcessingControls();
            tabCobro.Resize -= TabCobro_ResizeForProcessing;
            tabCobro.Resize += TabCobro_ResizeForProcessing;
        }

        private void TabCobro_ResizeForProcessing(object? sender, EventArgs e)
            => RepositionCobroProcessingControls();

        private void RepositionCobroProcessingControls()
        {
            if (lblCobroProcesando == null || progressCobroProcesando == null || tabCobro == null) return;
            const int rightMargin = 24;
            const int bottomMargin = 24;
            var x = Math.Max(16, tabCobro.ClientSize.Width - progressCobroProcesando.Width - rightMargin);
            var yBar = Math.Max(16, tabCobro.ClientSize.Height - progressCobroProcesando.Height - bottomMargin);
            progressCobroProcesando.Location = new Point(x, yBar);
            lblCobroProcesando.Location = new Point(x, Math.Max(8, yBar - lblCobroProcesando.Height - 8));
        }

        private void SetCobroProcesando(bool procesando)
        {
            if (lblCobroProcesando != null)
                lblCobroProcesando.Visible = procesando;
            if (progressCobroProcesando != null)
            {
                progressCobroProcesando.Visible = procesando;
                progressCobroProcesando.Style = ProgressBarStyle.Marquee;
                progressCobroProcesando.MarqueeAnimationSpeed = procesando ? 25 : 0;
            }

            btnConfirmarCobro.Enabled = !procesando;
            btnConfirmarCobro.Text = procesando ? "Procesando..." : "Confirmar y Cerrar";
            UseWaitCursor = procesando;
            Cursor = procesando ? Cursors.WaitCursor : Cursors.Default;
            if (procesando)
            {
                RepositionCobroProcessingControls();
                Application.DoEvents();
            }
        }

        private void GuardarConfigUI()
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            AuxDbInitializer.UpsertConfig(aux, "BASCULA_PUERTO", txtPuertoCom.Text);
            AuxDbInitializer.UpsertConfig(aux, "IMPRESORA_TICKET", cboImpresora.Text);
            AuxDbInitializer.UpsertConfig(aux, "ALMACEN_DEFAULT", cboAlmacen.Text);
            AuxDbInitializer.UpsertConfig(aux, "LISTA_PRECIOS", cboListaPrecios.Text);
            AuxDbInitializer.UpsertConfig(aux, "NEGOCIO_NOMBRE", txtNombreNegocio?.Text ?? string.Empty);
            AuxDbInitializer.UpsertConfig(aux, "TICKET_ANCHO_MM", cboTicketAncho?.Text ?? "58");
        }

        private void btnConfirmarCobro_Click(object? sender, EventArgs e)
        {
            if (_procesandoConfirmacionCobro)
                return;

            _procesandoConfirmacionCobro = true;
            SetCobroProcesando(true);

            try
            {
                if (_idPedidoActual == null)
                {
                    MessageBox.Show("No hay pedido abierto.", "Cobro",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var forma = GetFormaPagoSatSeleccionada();
                var metodo = (cboMetodoPago.SelectedValue as string) ?? "PUE";
                var uso = (cboUsoCFDI.SelectedValue as string) ?? "G03";

                decimal recibido = 0m;
                decimal.TryParse(txtImporteRecibido.Text, out recibido);

                decimal eff = 0m, tar = 0m;

                if (FormaPagoEsEfectivo()) // EFECTIVO
                {
                    if (recibido < _totalCobroActual)
                    {
                        MessageBox.Show($"Pago insuficiente. Total ${_totalCobroActual:N2}.", "Cobro",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    eff = recibido;
                    tar = 0m;
                }
                else
                {
                    // Tarjetas/transferencia: cobramos el total por ese medio
                    eff = 0m;
                    tar = _totalCobroActual;
                }

                if (chkFacturarAhora.Checked)
                {
                    if (string.IsNullOrWhiteSpace(txtRFC.Text) || string.IsNullOrWhiteSpace(txtRazon.Text))
                    {
                        MessageBox.Show("Captura RFC y Razón social para facturar.", "CFDI",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (txtCodigoPostalCfdi == null || !Regex.IsMatch((txtCodigoPostalCfdi.Text ?? string.Empty).Trim(), @"^\d{5}$"))
                    {
                        MessageBox.Show("Captura un código postal válido para facturar.", "CFDI",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (cboRegFiscalCfdi == null || cboRegFiscalCfdi.SelectedIndex < 0)
                    {
                        MessageBox.Show("Selecciona el régimen fiscal del cliente.", "CFDI",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                var almacenCobro = string.IsNullOrWhiteSpace(cboAlmacen.Text)
                    ? (AuxDbInitializer.GetConfig("ALMACEN_DEFAULT") ?? "1")
                    : cboAlmacen.Text;

                var request = new CobroRequest
                {
                    Efectivo = eff,
                    Tarjeta = tar,
                    ReferenciaTarjeta = null,
                    FacturarAhora = chkFacturarAhora.Checked,
                    Rfc = txtRFC.Text.Trim(),
                    RazonSocial = txtRazon.Text.Trim(),
                    CodigoPostal = txtCodigoPostalCfdi?.Text.Trim(),
                    RegFiscal = (cboRegFiscalCfdi?.SelectedValue as string) ?? string.Empty,
                    UsoCfdi = uso,
                    MetodoPago = metodo,
                    FormaPago = forma,
                    ClienteClaveSae = null,
                    Almacen = almacenCobro
                };

                var preparation = SaeCobroService.PrepararDesdePedidoAuxiliar(_idPedidoActual.Value, request);
                if (!preparation.Validation.IsValid)
                {
                    MessageBox.Show(
                        "No se puede confirmar el cobro porque la información actual todavía no está lista para postearse correctamente a SAE.\n\n" +
                        preparation.Validation.ToDisplayText(),
                        "Cobro bloqueado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var saePost = SaeCobroService.PostearCobroDesdePedidoAuxiliar(_idPedidoActual.Value, request);
                var res = AuxRepo.CobrarPedido(_idPedidoActual.Value, eff, tar, referenciaTarjeta: null);

                var cambioTxt = lblCambio.Text;
                var mesaLiberadaId = _mesaSeleccionada?.Id;
                var mesaNombreTicket = _mesaSeleccionada?.Nombre ?? string.Empty;
                var meseroNombreTicket = _mesaSeleccionada?.MeseroNombre ?? string.Empty;
                LimpiarPedidoActualEnPantalla();
                _idMesaTurnoActual = null;
                CargarMesasDesdeAux();
                RecargarMesasCobroSelector();

                if (mesaLiberadaId.HasValue)
                {
                    var mesaLibre = _mesas.FirstOrDefault(x => x.Id == mesaLiberadaId.Value);
                    if (mesaLibre != null)
                    {
                        _mesaSeleccionada = mesaLibre;
                        lblMesaSel.Text = $"Seleccionada: {mesaLibre.Nombre} ({mesaLibre.Estado})";
                    }
                }

                var resumen = $"Total: ${res.Total:N2}\n" +
                              $"Forma: {((ComboItem)cboFormaPago.SelectedItem).Texto}\n" +
                              (forma == "01" ? $"Recibido: ${recibido:N2}\n{cambioTxt}" : "") +
                              $"\nNota SAE: {saePost.NotaVentaDoc}  Serie {saePost.NotaVentaSerie}  Folio {saePost.NotaVentaFolio}" +
                              (chkFacturarAhora.Checked
                                  ? $"\nFactura SAE: {saePost.FacturaDoc}  Serie {saePost.FacturaSerie}  Folio {saePost.FacturaFolio}\nCFDI: RFC {txtRFC.Text.Trim()}  Uso {uso}  Método {metodo}"
                                  : "") +
                              $"\nCliente SAE: {saePost.ClienteClaveSae}";

                var avisos = new List<string>();
                avisos.AddRange(preparation.Validation.Warnings);
                avisos.AddRange(saePost.Warnings);

                if (TryPrepareAndPrintTicket(saePost.NotaVentaDoc, saePost.NotaVentaFolio, res.Total, recibido,
                                             forma == "01" ? Math.Max(0m, recibido - res.Total) : 0m,
                                             mesaNombreTicket, meseroNombreTicket,
                                             out var ticketWarn) && !string.IsNullOrWhiteSpace(ticketWarn))
                {
                    avisos.Add(ticketWarn);
                }
                else if (!string.IsNullOrWhiteSpace(ticketWarn))
                {
                    avisos.Add(ticketWarn);
                }

                if (avisos.Count > 0)
                    resumen += "\n\nAvisos SAE:\n- " + string.Join("\n- ", avisos.Distinct());

                MessageBox.Show("Cobro realizado.\n\n" + resumen, "Cobro",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                tabMain.SelectedTab = tabMesas;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _procesandoConfirmacionCobro = false;
                SetCobroProcesando(false);
            }
        }


        private bool TryPrepareAndPrintTicket(string notaDoc, int folio, decimal total, decimal recibido, decimal cambio, string mesaNombre, string meseroNombre, out string? warning)
        {
            warning = null;
            try
            {
                var ticket = BuildTicketDataFromSae(notaDoc, folio, total, recibido, cambio, mesaNombre, meseroNombre);
                _ultimoTicketData = ticket;

                var printerName = (AuxDbInitializer.GetConfig("IMPRESORA_TICKET") ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(printerName))
                {
                    warning = "No hay impresora de tickets configurada. El ticket quedó listo para reimpresión.";
                    return false;
                }

                if (!int.TryParse((AuxDbInitializer.GetConfig("TICKET_ANCHO_MM") ?? "58").Trim(), out var widthMm))
                    widthMm = 58;

                if (!TicketPrinter.TryPrint(ticket, printerName, widthMm, out var error))
                {
                    warning = string.IsNullOrWhiteSpace(error)
                        ? "No fue posible imprimir el ticket."
                        : error;
                    return false;
                }

                try { SaeCobroService.MarcarNotaVentaImpresa(ticket.CveDoc); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                warning = "No fue posible preparar/imprimir el ticket: " + ex.Message;
                return false;
            }
        }

        private void ReimprimirUltimoTicket()
        {
            if (_ultimoTicketData == null)
            {
                MessageBox.Show("Aún no hay un ticket listo para reimprimir.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printerName = (AuxDbInitializer.GetConfig("IMPRESORA_TICKET") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show("No hay impresora de tickets configurada.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse((AuxDbInitializer.GetConfig("TICKET_ANCHO_MM") ?? "58").Trim(), out var widthMm))
                widthMm = 58;

            if (!TicketPrinter.TryPrint(_ultimoTicketData, printerName, widthMm, out var error))
            {
                MessageBox.Show(error ?? "No fue posible reimprimir el ticket.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try { SaeCobroService.MarcarNotaVentaImpresa(_ultimoTicketData.CveDoc); } catch { }
            MessageBox.Show("Ticket enviado a impresión.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private TicketDocumentData BuildTicketDataFromSae(string notaDoc, int folio, decimal total, decimal recibido, decimal cambio, string mesaNombre, string meseroNombre)
        {
            using var con = SaeDb.GetOpenConnection();
            var factv = SaeDb.GetTableName(con, "FACTV");
            var parFactv = SaeDb.GetTableName(con, "PAR_FACTV");
            var inve = SaeDb.GetTableName(con, "INVE");
            var paramDatosEmp = SaeDb.GetTableName(con, "PARAM_DATOSEMP");
            var paramDomFiscal = SaeDb.GetTableName(con, "PARAM_DOMFISCAL");

            var negocio = (AuxDbInitializer.GetConfig("NEGOCIO_NOMBRE") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(negocio)) negocio = "NEGOCIO";
            var rfcNegocio = string.Empty;
            var tiendaNumero = "1";
            var domicilioFiscalLineas = new List<string>();

            DateTime fechaDoc = DateTime.Today;
            DateTime? fechaElab = null;
            decimal subtotal = 0m, descuento = 0m, impuesto = 0m;
            string? formaPagoTexto = null;

            using (var cmd = new FbCommand($@"
SELECT FECHA_DOC, FECHAELAB, CAN_TOT, DES_TOT, DES_FIN,
       IMP_TOT1, IMP_TOT2, IMP_TOT3, IMP_TOT4, IMP_TOT5, IMP_TOT6, IMP_TOT7, IMP_TOT8,
       FORMADEPAGOSAT
FROM {factv}
WHERE CVE_DOC=@D", con))
            {
                cmd.Parameters.Add("@D", FbDbType.VarChar, 20).Value = notaDoc;
                using var rd = cmd.ExecuteReader();
                if (!rd.Read())
                    throw new InvalidOperationException("No se encontró la nota de venta en SAE para imprimir el ticket.");

                if (!rd.IsDBNull(0)) fechaDoc = Convert.ToDateTime(rd.GetValue(0));
                if (!rd.IsDBNull(1)) fechaElab = Convert.ToDateTime(rd.GetValue(1));
                subtotal = rd.IsDBNull(2) ? 0m : Convert.ToDecimal(rd.GetValue(2));
                var desTot = rd.IsDBNull(3) ? 0m : Convert.ToDecimal(rd.GetValue(3));
                var desFin = rd.IsDBNull(4) ? 0m : Convert.ToDecimal(rd.GetValue(4));
                descuento = Math.Round(desTot + desFin, 2);
                decimal sumaImp = 0m;
                for (int i = 5; i <= 12; i++)
                    sumaImp += rd.IsDBNull(i) ? 0m : Convert.ToDecimal(rd.GetValue(i));
                impuesto = Math.Round(sumaImp, 2);
                formaPagoTexto = rd.IsDBNull(13) ? null : rd.GetString(13).Trim();
            }

            if (!string.IsNullOrWhiteSpace(paramDatosEmp))
            {
                using var cmdEmp = new FbCommand($@"
SELECT NUM_EMP, RFC
FROM {paramDatosEmp}
ORDER BY NUM_EMP", con);
                using var rdEmp = cmdEmp.ExecuteReader();
                if (rdEmp.Read())
                {
                    tiendaNumero = rdEmp.IsDBNull(0) ? "1" : Convert.ToString(rdEmp.GetValue(0))?.Trim() ?? "1";
                    rfcNegocio = rdEmp.IsDBNull(1) ? string.Empty : rdEmp.GetString(1).Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(paramDomFiscal))
            {
                using var cmdDom = new FbCommand($@"
SELECT CALLE, NUMERO_EXT, NUMERO_INT, COLONIA, LOCALIDAD, MUNICIPIO, ESTADO, PAIS, CP
FROM {paramDomFiscal}
ORDER BY NUM_EMP", con);
                using var rdDom = cmdDom.ExecuteReader();
                if (rdDom.Read())
                {
                    string calle = rdDom.IsDBNull(0) ? string.Empty : rdDom.GetString(0).Trim();
                    string numExt = rdDom.IsDBNull(1) ? string.Empty : rdDom.GetString(1).Trim();
                    string numInt = rdDom.IsDBNull(2) ? string.Empty : rdDom.GetString(2).Trim();
                    string colonia = rdDom.IsDBNull(3) ? string.Empty : rdDom.GetString(3).Trim();
                    string localidad = rdDom.IsDBNull(4) ? string.Empty : rdDom.GetString(4).Trim();
                    string municipio = rdDom.IsDBNull(5) ? string.Empty : rdDom.GetString(5).Trim();
                    string estado = rdDom.IsDBNull(6) ? string.Empty : rdDom.GetString(6).Trim();
                    string pais = rdDom.IsDBNull(7) ? string.Empty : rdDom.GetString(7).Trim();
                    string cp = rdDom.IsDBNull(8) ? string.Empty : rdDom.GetString(8).Trim();

                    var linea1 = string.Join(" ", new[] { calle, numExt, string.IsNullOrWhiteSpace(numInt) ? string.Empty : "INT " + numInt }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(linea1)) domicilioFiscalLineas.Add(linea1);
                    if (!string.IsNullOrWhiteSpace(colonia)) domicilioFiscalLineas.Add("Col. " + colonia);
                    var linea3 = string.Join(", ", new[] { string.IsNullOrWhiteSpace(cp) ? string.Empty : "CP " + cp, municipio, estado }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(linea3)) domicilioFiscalLineas.Add(linea3);
                    var linea4 = string.Join(", ", new[] { localidad, pais }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(linea4)) domicilioFiscalLineas.Add(linea4);
                }
            }

            var lineas = new List<TicketLineItem>();
            using (var cmd = new FbCommand($@"
SELECT p.NUM_PAR, p.CANT, p.UNI_VENTA, COALESCE(i.DESCR, p.CVE_ART) AS DESCR, p.PREC, p.TOT_PARTIDA
FROM {parFactv} p
LEFT JOIN {inve} i ON i.CVE_ART = p.CVE_ART
WHERE p.CVE_DOC=@D
ORDER BY p.NUM_PAR", con))
            {
                cmd.Parameters.Add("@D", FbDbType.VarChar, 20).Value = notaDoc;
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lineas.Add(new TicketLineItem
                    {
                        Cantidad = rd.IsDBNull(1) ? 0m : Convert.ToDecimal(rd.GetValue(1)),
                        Unidad = rd.IsDBNull(2) ? string.Empty : rd.GetString(2).Trim(),
                        Descripcion = rd.IsDBNull(3) ? string.Empty : rd.GetString(3).Trim(),
                        PrecioUnitario = rd.IsDBNull(4) ? 0m : Convert.ToDecimal(rd.GetValue(4)),
                        Importe = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5))
                    });
                }
            }

            return new TicketDocumentData
            {
                Negocio = negocio,
                RfcNegocio = rfcNegocio,
                TiendaNumero = tiendaNumero,
                DomicilioFiscalLineas = domicilioFiscalLineas,
                CveDoc = notaDoc,
                FolioTexto = (notaDoc ?? string.Empty).TrimStart(),
                Fecha = fechaDoc.Date,
                Hora = fechaElab?.ToString("hh:mm:ss tt") ?? DateTime.Now.ToString("hh:mm:ss tt"),
                AtendidoPor = string.IsNullOrWhiteSpace(meseroNombre) ? "ADMINISTRADOR" : meseroNombre.ToUpperInvariant(),
                Mesa = mesaNombre,
                Subtotal = subtotal,
                Descuento = descuento,
                Impuesto = impuesto,
                Total = total,
                Efectivo = recibido,
                Cambio = cambio,
                FormaPagoTexto = string.IsNullOrWhiteSpace(formaPagoTexto) ? "01" : formaPagoTexto,
                Lineas = lineas
            };
        }

        private void CargarNotasReimpresionConfig()
        {
            if (cboTicketReprintNv == null) return;
            try
            {
                using var con = SaeDb.GetOpenConnection();
                var factv = SaeDb.GetTableName(con, "FACTV");
                var items = new List<TicketNotaOption>();
                using var cmd = new FbCommand($@"SELECT FIRST 100 CVE_DOC, FOLIO, FECHA_DOC, IMPORTE FROM {factv} WHERE TIP_DOC='V' ORDER BY FECHA_DOC DESC, FOLIO DESC", con);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    items.Add(new TicketNotaOption
                    {
                        CveDoc = rd.IsDBNull(0) ? string.Empty : Convert.ToString(rd.GetValue(0)) ?? string.Empty,
                        Folio = rd.IsDBNull(1) ? 0 : Convert.ToInt32(rd.GetValue(1)),
                        Fecha = rd.IsDBNull(2) ? DateTime.Today : Convert.ToDateTime(rd.GetValue(2)),
                        Total = rd.IsDBNull(3) ? 0m : Convert.ToDecimal(rd.GetValue(3))
                    });
                }
                cboTicketReprintNv.BeginUpdate();
                cboTicketReprintNv.DataSource = null;
                cboTicketReprintNv.Items.Clear();
                foreach (var it in items) cboTicketReprintNv.Items.Add(it);
                cboTicketReprintNv.EndUpdate();
                if (items.Count > 0) cboTicketReprintNv.SelectedIndex = 0;
            }
            catch
            {
                cboTicketReprintNv.DataSource = null;
                cboTicketReprintNv.Items.Clear();
            }
        }

        private void ReimprimirTicketSeleccionadoConfig()
        {
            if (cboTicketReprintNv?.SelectedItem is not TicketNotaOption item)
            {
                MessageBox.Show("Selecciona una nota de venta para reimprimir.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printerName = (AuxDbInitializer.GetConfig("IMPRESORA_TICKET") ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show("No hay impresora de tickets configurada.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse((AuxDbInitializer.GetConfig("TICKET_ANCHO_MM") ?? "58").Trim(), out var widthMm))
                widthMm = 58;

            TicketDocumentData ticket;
            try
            {
                ticket = BuildTicketDataFromSae(item.CveDoc, item.Folio, item.Total, item.Total, 0m, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible preparar el ticket: " + ex.Message, "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!TicketPrinter.TryPrint(ticket, printerName, widthMm, out var error))
            {
                MessageBox.Show(error ?? "No fue posible reimprimir el ticket.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try { SaeCobroService.MarcarNotaVentaImpresa(ticket.CveDoc); } catch { }
            MessageBox.Show("Ticket enviado a impresión.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // aaaa si
        private void btnGuardarConfig_Click(object sender, EventArgs e)
        {

            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            AuxDbInitializer.UpsertConfig(aux, "IMPRESORA_TICKET", cboImpresora.Text);
            AuxDbInitializer.UpsertConfig(aux, "BASCULA_PUERTO", txtPuertoCom.Text);
            AuxDbInitializer.UpsertConfig(aux, "ALMACEN_DEFAULT", cboAlmacen.Text);
            AuxDbInitializer.UpsertConfig(aux, "LISTA_PRECIOS", cboListaPrecios.Text);
            AuxDbInitializer.UpsertConfig(aux, "NEGOCIO_NOMBRE", txtNombreNegocio?.Text ?? string.Empty);
            AuxDbInitializer.UpsertConfig(aux, "TICKET_ANCHO_MM", cboTicketAncho?.Text ?? "58");
            MessageBox.Show("Configuración guardada.");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _timerBascula.Enabled = false;
            _timerBascula.Dispose();
            // Permite que Windows cierre en apagado/sesión (opcional)
            if (e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing)
                return;

            // 1) Regla estricta: NO se puede salir si hay mesas OCUPADAS o EN_CUENTA
            if (AuxRepo.ExistenMesasOcupadasOEnCuenta())
            {
                MessageBox.Show(
                    "Hay mesas OCUPADAS o EN_CUENTA.\n" +
                    "Libera o cierra las mesas antes de salir.",
                    "No se puede salir",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                e.Cancel = true;                 // ← clave: NO salir
                                                 // (Opcional) enfoca la pestaña de Mesas si quieres:
                                                 // tabControlPrincipal.SelectedTab = tabMesas;
                return;
            }

            // 2) Si no hay mesas abiertas pero sí hay un turno abierto, pregunta si lo cierras
            var idTurno = AuxRepo.GetTurnoAbiertoId();
            if (idTurno != null)
            {
                var resp = MessageBox.Show(
                    "Para salir es necesario CERRAR el turno actual.\n\n" +
                    "Se cerrará el turno ahora. ¿Continuar?",
                    "Cerrar turno",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);

                if (resp == DialogResult.Cancel)
                {
                    e.Cancel = true;     // Cancelar salida
                    return;
                }

                try
                {
                    AuxRepo.CerrarTurno(idTurno.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "No se pudo cerrar el turno",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    e.Cancel = true;     // Si no cierra turno, no sale
                    return;
                }
            }

            // 3) Si no hay turno abierto, simplemente salir

        }

        private bool CambiarEstadoMesa(Mesa m, MesaEstado nuevo)
        {
            var ok = (m.Estado, nuevo) switch
            {
                (MesaEstado.LIBRE, MesaEstado.OCUPADA) => true,
                (MesaEstado.OCUPADA, MesaEstado.EN_CUENTA) => true,
                (MesaEstado.EN_CUENTA, MesaEstado.CERRADA) => true,
                (MesaEstado.OCUPADA, MesaEstado.CERRADA) => true, // por cancelación
                (MesaEstado.CERRADA, MesaEstado.LIBRE) => true,   // reapertura
                _ => false
            };
            if (!ok) { MessageBox.Show($"Transición inválida: {m.Estado} → {nuevo}"); return false; }
            m.Estado = nuevo; dgvMesas.Refresh(); return true;
        }


        private void lbInvArticulos_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActualizarContextoInventarioSeleccionado();
        }


        private void ActualizarContextoInventarioSeleccionado()
        {
            if (lbInvArticulos.SelectedItem is not InvArticulo art)
            {
                SetDynamicPlaceholder(txtInvPesoGr, "Cantidad");
                SetDynamicPlaceholder(txtInvCostoKg, "Costo unitario");
                lblInvKg.Text = "0.000";
                UpdateScaleTimer();
                return;
            }

            var captureName = art.UnidadCaptura;
            var baseName = art.UnidadBase;
            SetDynamicPlaceholder(txtInvPesoGr, $"Cantidad ({captureName})");
            SetDynamicPlaceholder(txtInvCostoKg, $"Costo por {captureName}");
            var costoCaptura = SaeCatalogAdmin.ConvertBaseCostToCapture(baseName, captureName, factor: art.FactorConversion, costoBase: art.CostoProm);
            txtInvCostoKg.Text = costoCaptura > 0m ? costoCaptura.ToString("N3") : "0";
            ActualizarVistaCantidadInventario();
            UpdateScaleTimer();
        }

        private void ActualizarVistaCantidadInventario()
        {
            var art = lbInvArticulos.SelectedItem as InvArticulo;
            var capture = art?.UnidadCaptura ?? "kg";
            var baseUnit = art?.UnidadBase ?? "gr";
            var factor = art?.FactorConversion ?? 1000m;
            var usaBascula = art?.UsaBascula ?? false;

            if (decimal.TryParse(txtInvPesoGr.Text, out var cantidad) && cantidad >= 0)
            {
                var baseQty = decimal.Round(SaeCatalogAdmin.ConvertCaptureQtyToBase(baseUnit, capture, factor, cantidad), 3, MidpointRounding.AwayFromZero);
                lblInvKg.Text = $"{FormatearCantidadUnidad(baseUnit, baseQty)} {baseUnit}";
            }
            else
            {
                lblInvKg.Text = $"0 {(baseUnit)}";
            }

            chkInvSimularBascula.Text = usaBascula
                ? "Simular báscula (Báscula • Ctrl+B)"
                : $"Captura manual ({capture} → {baseUnit})";
        }

        private static string FormatearCantidadUnidad(string unidad, decimal cantidad)
        {
            var u = (unidad ?? string.Empty).Trim().ToLowerInvariant();
            return u == "pz" ? cantidad.ToString("N0") : cantidad.ToString("N3");
        }

        private static void SetDynamicPlaceholder(System.Windows.Forms.TextBox tb, string text)
        {
            try
            {
                var prop = tb.GetType().GetProperty("PlaceholderText");
                if (prop != null) prop.SetValue(tb, text);
            }
            catch { }
        }

        // Botón "Aplicar costo a todos"
        private void btnInvAplicarCostoTodos_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtInvCostoKg.Text, out var c) || c < 0) { MessageBox.Show("Costo inválido."); return; }
            foreach (var it in _invEntradas) it.CostoUnitBase = c;
            dgvInvCaptura.Refresh(); RecalcularTotalesInventario();
        }


        private string GetSaePathFromAuxConfig()
        {
            string auxPath;
            using (var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1"))
            using (var cmd = new FbCommand("SELECT VALOR FROM CONFIG WHERE CLAVE='SAE_FDB'", auxConn))
            {
                var o = cmd.ExecuteScalar();
                var path = o?.ToString();
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("CONFIG.SAE_FDB está vacío. Configura la ruta de SAE en la pestaña Configuración.");
                return path;
            }
        }

        private void btnCfgMesas_Click(object sender, EventArgs e)
        {
            using (var f = new FormMesasConfig())
            {
                f.ShowDialog(this);
                CargarMesasDesdeAux(); // refresca grilla al cerrar
            }
        }

        private void btnCfgMeseros_Click(object sender, EventArgs e)
        {
            using (var f = new FormMeserosConfig())
            {
                f.ShowDialog(this);
                CargarMeserosDesdeAux(); // refresca combo al cerrar
            }
        }

        private void btnCfgIngredientes_Click(object sender, EventArgs e)
        {
            using (var f = new FormIngredientesConfig())
            {
                f.ShowDialog(this);
                CargarInvArticulosDesdeSAE();
                RefrescarCentroAlertas(true);
            }
        }

        private void btnAbrirMesa_Click(object sender, EventArgs e)
        {
            AbrirAtenderMesa();
        }


        private void btnAsignarMesero_Click(object sender, EventArgs e)
        {
            EnsureMesaSeleccionada();
            var mSel = MesaSeleccionada();     // ← renombrado (antes era 'mesa')
            if (mSel == null)
            {
                MessageBox.Show("Selecciona una mesa.", "Mesas",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (mSel.Estado != MesaEstado.LIBRE)
            {
                MessageBox.Show("La mesa no está LIBRE. No puedes cambiar el mesero.",
                                "Mesas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (cboMesero.SelectedItem is not Mesero mesero)
            {
                MessageBox.Show("Selecciona un mesero.", "Mesas",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // asigna y refresca
            mSel.MeseroId = mesero.Id;
            mSel.MeseroNombre = mesero.Nombre;

            RefrescarFilaMesaActual(); // ← fuerza repintado inmediato
            lblMesaSel.Text = $"Seleccionada: {mSel.Nombre} ({mSel.Estado})";
            dgvMesas.Refresh();
            ActualizarHabilitacionMeseroSegunMesa();
        }


        private void btnLiberarMesa_Click(object sender, EventArgs e)
        {
            var mesa = MesaSeleccionada();
            if (mesa == null)
            {
                MessageBox.Show("Selecciona una mesa.", "Liberar mesa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (mesa.Estado == MesaEstado.LIBRE)
            {
                MessageBox.Show("La mesa ya está LIBRE.", "Liberar mesa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dr = MessageBox.Show($"¿Poner la mesa '{mesa.Nombre}' en LIBRE?\n" +
                                     $"(Se cerrará el registro activo y se cancelará el pedido en curso, si lo hay)",
                                     "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            try
            {
                AuxRepo.LiberarMesa(mesa.Id);

                CargarMesasDesdeAux();
                RecargarMesasCobroSelector();
                LimpiarPedidoActualEnPantalla();

                // Habilitación (ya puedes asignar mesero)
                ActualizarHabilitacionMeseroSegunMesa();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo liberar la mesa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }









        private void CargarPedidoDesdeDb()
        {
            if (_idPedidoActual == null)
            {
                LimpiarPedidoActualEnPantalla();
                RefrescarCentroAlertas(false);
                return;
            }

            var dets = AuxRepo.ListarPedidoDet(_idPedidoActual.Value);
            var lista = new BindingList<PedidoDet>();
            foreach (var d in dets)
            {
                lista.Add(new PedidoDet
                {
                    IdDet = d.IdDet,
                    Partida = lista.Count + 1,
                    Clave = d.ClaveArticulo,
                    Nombre = SaeDb.ObtenerDescripcionArticulo(d.ClaveArticulo),
                    Cantidad = d.Cantidad,
                    PesoGr = d.PesoGr,
                    PrecioUnit = d.PrecioUnit,
                    RequierePeso = d.PesoGr.HasValue && d.PesoGr.Value > 0
                });
            }

            if (_pedidoActual == null)
                _pedidoActual = new Pedido { Id = _idPedidoActual.Value };

            _pedidoActual.Detalles = lista;
            dgvPedido.DataSource = _pedidoActual.Detalles;

            // Totales (desde BD para que coincidan)
            var (sub, imp, tot) = AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value, TASA_IVA);
            lblTotales.Text = FormatearResumenFiscal(sub, imp, tot);
            RefrescarCentroAlertas(false);
            AplicarRestriccionDisponibilidadPedidoActual();
        }





        private void QuitarLineaSeleccionada()
        {
            if (dgvPedido.CurrentRow?.DataBoundItem is not PedidoDet sel) return;

            // Persistido en BD
            if (_idPedidoActual != null && sel.IdDet.HasValue)
            {
                AuxRepo.EliminarPedidoDet(sel.IdDet.Value);
                AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value, TASA_IVA);
                CargarPedidoDesdeDb();
                return;
            }

            // Solo en memoria
            _pedidoActual?.Detalles.Remove(sel);
            if (_pedidoActual != null)
            {
                int i = 1; foreach (var x in _pedidoActual.Detalles) x.Partida = i++;
            }
            RecalcularTotales();
        }



        private void dgvPedido_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) QuitarLineaSeleccionada();
        }

        private void btnQuitarLinea_Click(object sender, EventArgs e)
        {
            QuitarLineaSeleccionada();
        }


        private BindingList<SaeDb.RecetaItemDto> _recetaActual = new();


        private void lbPlatillos_SelectedIndexChanged(object sender, EventArgs e)
        {
            MostrarRecetaPlatilloSeleccionado();
        }

        private void MostrarRecetaPlatilloSeleccionado()
        {
            if (lbPlatillos.SelectedItem is not Platillo p)
            {
                _recetaActual.Clear();
                return;
            }

            // usa el almacén configurado (si ya lo tienes en un combo); por ahora 1
            int? alm = 1;
            if (int.TryParse(cboAlmacen.Text, out var a)) alm = a;

            var receta = SaeDb.ListarReceta(p.Clave, almacen: alm);
            _recetaActual = new BindingList<SaeDb.RecetaItemDto>(receta);
            dgvReceta.DataSource = _recetaActual;
        }


        // Devuelve la mesa actualmente seleccionada en el grid

        private Mesa? MesaSeleccionada()
        {
            return dgvMesas.CurrentRow?.DataBoundItem as Mesa;

        }
        // Fuerza que haya una fila seleccionada y sincroniza UI (label + botones)
        private void EnsureMesaSeleccionada()
        {
            if (dgvMesas.DataSource == null || dgvMesas.Rows.Count == 0)
            {
                _mesaSeleccionada = null;
                lblMesaSel.Text = "Sin mesa seleccionada";
                ActualizarHabilitacionMeseroSegunMesa();
                return;
            }

            if (dgvMesas.CurrentRow == null)
            {
                dgvMesas.ClearSelection();
                dgvMesas.CurrentCell = dgvMesas.Rows[0].Cells[0];
                dgvMesas.Rows[0].Selected = true;
            }

            _mesaSeleccionada = MesaSeleccionada();
            lblMesaSel.Text = _mesaSeleccionada != null
                ? $"Seleccionada: {_mesaSeleccionada.Nombre} ({_mesaSeleccionada.Estado})"
                : "Sin mesa seleccionada";

            ActualizarHabilitacionMeseroSegunMesa();
        }


        private void RefrescarFilaMesaActual()
        {
            if (dgvMesas.CurrentRow != null)
            {
                int r = dgvMesas.CurrentRow.Index;
                dgvMesas.InvalidateRow(r);
            }
            dgvMesas.Refresh();
        }



        private void Cobro_RecalcularCambio(object? sender, EventArgs e)
        {
            decimal total = _totalCobroActual, eff = 0m, tar = 0m;
            decimal.TryParse(txtCobroEfectivo.Text, out eff);
            decimal.TryParse(txtCobroTarjeta.Text, out tar);

            var cambio = Math.Max(0m, Math.Round((eff + tar) - total, 2));
            lblCobroCambio.Text = $"Cambio: ${cambio:N2}";
        }


        // Recalcula el resumen del "cobro rápido" (split efectivo/tarjeta)
        // Nota: este flujo es independiente del flujo CFDI (txtImporteRecibido/lblCambio).
        private void CobroRapido_Recalcular_UI()
        {
            // Total actual (lo controla IrACobro / RecalcularTotalesPedido)
            lblCobroTotal.Text = FormatearTotalCobro(_totalCobroActual);

            decimal eff = 0m, tar = 0m;
            decimal.TryParse(txtCobroEfectivo.Text, out eff);
            decimal.TryParse(txtCobroTarjeta.Text, out tar);

            eff = Math.Max(0m, eff);
            tar = Math.Max(0m, tar);

            // No permitir tarjeta > total
            tar = Math.Min(tar, _totalCobroActual);

            // La tarjeta cubre primero; el efectivo cubre el resto
            var restante = _totalCobroActual - tar;
            var cambio = Math.Max(0m, Math.Round(eff - restante, 2));
            lblCobroCambio.Text = $"Cambio: ${cambio:N2}";
        }


        [Obsolete("Se mantiene por compatibilidad visual. Ambos botones de la pestaña Cobro usan ahora el mismo flujo SAE.")]
        private void btnCobroConfirmar_Click(object? sender, EventArgs e)
        {
            btnConfirmarCobro_Click(sender, e);
        }




        private void LimpiarPedidoActualEnPantalla()
        {
            _pedidoActual = null;
            _idPedidoActual = null;
            dgvPedido.DataSource = null;
            lblTotales.Text = FormatearResumenFiscal(0m, 0m, 0m);
            _totalCobroActual = 0m;
            lblResumenCobro.Text = FormatearResumenFiscal(0m, 0m, 0m);
            lblCobroTotal.Text = FormatearTotalCobro(0m);
            lblCambio.Text = "Cambio: $0.00";
            lblCobroMesa.Text = "Sin mesa seleccionada";
            ActualizarVisibilidadCamposCobro();
        }

        private void PrepararPantallaCobroDesdePedidoActual()
        {
            if (_idPedidoActual == null)
            {
                LimpiarPedidoActualEnPantalla();
                return;
            }

            var (sub, imp, tot) = AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value, TASA_IVA);
            _totalCobroActual = tot;

            lblCobroMesa.Text = _mesaSeleccionada == null
                ? "Sin mesa seleccionada"
                : $"Mesa {_mesaSeleccionada.Nombre}";

            lblResumenCobro.Text = FormatearResumenFiscal(sub, imp, tot);
            cboFormaPago.SelectedValue = "10";
            txtImporteRecibido.Enabled = true;
            txtImporteRecibido.Text = tot.ToString("0.00");
            Cobro_RecalcularCambio_UI(null, EventArgs.Empty);

            txtCobroEfectivo.Text = tot.ToString("0.00");
            txtCobroTarjeta.Text = "0.00";
            txtCobroRef.Text = string.Empty;
            CobroRapido_Recalcular_UI();
            ActualizarVisibilidadCamposCobro();
        }

        private void EnsureCobroMesaSelector()
        {
            if (_cboMesaCobroSelector != null && _lblMesaCobroSelector != null)
                return;

            _lblMesaCobroSelector = new Label
            {
                Name = "lblMesaCobroSelector",
                AutoSize = true,
                Text = "Mesa a cobrar",
                Location = new Point(343, 28)
            };

            _cboMesaCobroSelector = new ComboBox
            {
                Name = "cboMesaCobroSelector",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(343, 55),
                Size = new Size(255, 33)
            };
            _cboMesaCobroSelector.SelectedIndexChanged += CboMesaCobroSelector_SelectedIndexChanged;

            tabCobro.Controls.Add(_lblMesaCobroSelector);
            tabCobro.Controls.Add(_cboMesaCobroSelector);
            _lblMesaCobroSelector.BringToFront();
            _cboMesaCobroSelector.BringToFront();
        }

        private void RecargarMesasCobroSelector(int? mesaPreferida = null)
        {
            EnsureCobroMesaSelector();
            if (_cboMesaCobroSelector == null) return;

            var items = AuxRepo.ListarMesasConPedidoAbierto()
                .Select(x => new CobroMesaItem
                {
                    MesaId = x.IdMesa,
                    PedidoId = x.IdPedido,
                    Nombre = x.NombreMesa,
                    Estado = x.EstadoMesa
                })
                .ToList();

            _cargandoMesaCobro = true;
            _cboMesaCobroSelector.DataSource = null;
            _cboMesaCobroSelector.Items.Clear();
            foreach (var item in items) _cboMesaCobroSelector.Items.Add(item);
            _cboMesaCobroSelector.Enabled = items.Count > 0;
            _cargandoMesaCobro = false;

            if (items.Count == 0)
            {
                lblCobroMesa.Text = "Sin mesas pendientes de cobro";
                return;
            }

            var idx = mesaPreferida.HasValue
                ? items.FindIndex(x => x.MesaId == mesaPreferida.Value)
                : -1;
            if (idx < 0) idx = 0;
            _cboMesaCobroSelector.SelectedIndex = idx;
        }

        private void CboMesaCobroSelector_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cargandoMesaCobro || _cboMesaCobroSelector == null) return;
            if (_cboMesaCobroSelector.SelectedItem is not CobroMesaItem item) return;

            SeleccionarMesaEnGridPorId(item.MesaId);
            PrepararPantallaCobroDesdePedidoActual();
        }

        private void SeleccionarMesaEnGridPorId(int mesaId)
        {
            for (int i = 0; i < dgvMesas.Rows.Count; i++)
            {
                if (dgvMesas.Rows[i].DataBoundItem is Mesa m && m.Id == mesaId)
                {
                    dgvMesas.ClearSelection();
                    dgvMesas.CurrentCell = dgvMesas.Rows[i].Cells[0];
                    dgvMesas.Rows[i].Selected = true;
                    SeleccionarMesa();
                    EnsureMesaSeleccionada();
                    return;
                }
            }
        }

        private decimal ObtenerCantidadPedidaActualDePlatillo(string clavePlatillo)
        {
            if (_pedidoActual?.Detalles == null || string.IsNullOrWhiteSpace(clavePlatillo))
                return 0m;

            return _pedidoActual.Detalles
                .Where(x => string.Equals(x.Clave, clavePlatillo, StringComparison.OrdinalIgnoreCase))
                .Sum(x => Math.Max(0m, x.Cantidad));
        }

        private decimal ObtenerPorcionesRestantesParaPedidoActual(Platillo platillo)
        {
            if (platillo == null) return 0m;
            return Math.Max(0m, platillo.PorcionesDisponibles);
        }

        private static string FormatearPorcionesPedido(decimal valor)
        {
            return Math.Abs(valor - Math.Round(valor, 0)) < 0.0001m
                ? Math.Round(valor, 0).ToString("N0")
                : valor.ToString("N2");
        }

        private void RecalcularDisponibilidadPlatillosEnPantalla()
        {
            if (_platillos == null || _platillos.Count == 0)
                return;

            var reservadas = AuxRepo.ObtenerCantidadesPlatillosReservadas();

            foreach (var p in _platillos)
            {
                p.Disponible = p.DisponibleBase;
                p.Limitado = p.LimitadoBase;
                p.PorcionesDisponibles = p.PorcionesDisponiblesBase;
                p.MotivoDisponibilidad = p.MotivoDisponibilidadBase;

                if (!p.DisponibleBase || p.PorcionesDisponiblesBase <= 0m)
                    continue;

                reservadas.TryGetValue(p.Clave ?? string.Empty, out var yaReservadas);
                var restantes = Math.Max(0m, p.PorcionesDisponiblesBase - yaReservadas);
                p.PorcionesDisponibles = restantes;

                if (restantes < 1m)
                {
                    p.Disponible = false;
                    p.Limitado = false;
                    p.MotivoDisponibilidad = yaReservadas > 0m
                        ? $"Sin porciones disponibles. Ya están comprometidas en mesas abiertas ({FormatearPorcionesPedido(yaReservadas)} porciones)."
                        : (string.IsNullOrWhiteSpace(p.MotivoDisponibilidadBase) ? "Sin porciones disponibles." : p.MotivoDisponibilidadBase);
                }
                else if (restantes <= 5m)
                {
                    p.Disponible = true;
                    p.Limitado = true;
                    p.MotivoDisponibilidad = $"Solo hay {FormatearPorcionesPedido(restantes)} porciones disponibles considerando mesas abiertas.";
                }
                else
                {
                    p.Disponible = true;
                    p.Limitado = false;
                    p.MotivoDisponibilidad = string.Empty;
                }
            }

            string? claveSel = (lbPlatillos.SelectedItem as Platillo)?.Clave;
            lbPlatillos.DataSource = null;
            lbPlatillos.DataSource = _platillos;
            ReseleccionarPlatilloEnLista(claveSel);
        }

        private void AplicarRestriccionDisponibilidadPedidoActual()
        {
            RecalcularDisponibilidadPlatillosEnPantalla();
        }

        private decimal _totalCobroActual = 0m;
        private bool _procesandoConfirmacionCobro = false;
        private ComboBox? _cboMesaCobroSelector;
        private Label? _lblMesaCobroSelector;
        private bool _cargandoMesaCobro = false;
        private TextBox? txtCodigoPostalCfdi;
        private ComboBox? cboRegFiscalCfdi;

        private sealed class CobroMesaItem
        {
            public int MesaId { get; set; }
            public int PedidoId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Estado { get; set; } = string.Empty;
            public override string ToString() => $"{Nombre} ({Estado})";
        }

        private class ComboItem
        {
            public string Codigo { get; set; } = string.Empty;      // Código visible/operativo estilo SAE
            public string Texto { get; set; } = string.Empty;
            public string SatCodigo { get; set; } = string.Empty;   // Código SAT/documental que se guarda en SAE
            public override string ToString() => Texto;
        }

        private ComboItem? GetFormaPagoSeleccionada() => cboFormaPago?.SelectedItem as ComboItem;
        private string GetFormaPagoSatSeleccionada()
        {
            var item = GetFormaPagoSeleccionada();
            return string.IsNullOrWhiteSpace(item?.SatCodigo) ? "01" : item!.SatCodigo;
        }
        private string GetFormaPagoOperativaSeleccionada()
        {
            var item = GetFormaPagoSeleccionada();
            return string.IsNullOrWhiteSpace(item?.Codigo) ? "10" : item!.Codigo;
        }
        private bool FormaPagoEsEfectivo() => GetFormaPagoOperativaSeleccionada() == "10";
        private bool FormaPagoRequiereReferencia()
        {
            var op = GetFormaPagoOperativaSeleccionada();
            return op == "22";
        }


        private void EnsureCobroFacturaExtraControls()
        {
            if (txtCodigoPostalCfdi != null && cboRegFiscalCfdi != null)
                return;

            txtCodigoPostalCfdi = new TextBox
            {
                Name = "txtCodigoPostalCfdi",
                Enabled = false,
                Location = new Point(979, 318),
                Size = new Size(273, 31),
                MaxLength = 5
            };

            cboRegFiscalCfdi = new ComboBox
            {
                Name = "cboRegFiscalCfdi",
                Enabled = false,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(413, 371),
                Size = new Size(556, 33)
            };

            // Reacomoda Uso CFDI para dejar sitio a CP y Régimen fiscal.
            cboUsoCFDI.Location = new Point(979, 371);
            cboUsoCFDI.Size = new Size(273, 33);

            tabCobro.Controls.Add(txtCodigoPostalCfdi);
            tabCobro.Controls.Add(cboRegFiscalCfdi);
            txtCodigoPostalCfdi.BringToFront();
            cboRegFiscalCfdi.BringToFront();
        }

        private void InicializarTabCobro()
        {
            // Método de pago (CFDI) – PUE/PPD
            cboMetodoPago.DisplayMember = "Texto";
            cboMetodoPago.ValueMember = "Codigo";
            cboMetodoPago.DataSource = new List<ComboItem>
    {
        new() { Codigo = "PUE", Texto = "PUE - Una sola exhibición" },
        new() { Codigo = "PPD", Texto = "PPD - Parcialidades o diferido" },
    };
            cboMetodoPago.SelectedValue = "PUE";

            // Forma de pago operativa estilo SAE; internamente se mapea al código SAT/documental
            cboFormaPago.DisplayMember = "Texto";
            cboFormaPago.ValueMember = "Codigo";
            cboFormaPago.DataSource = new List<ComboItem>
    {
        new() { Codigo = "10", Texto = "10 - Efectivo", SatCodigo = "01" },
        new() { Codigo = "11", Texto = "11 - Cheque", SatCodigo = "02" },
        new() { Codigo = "15", Texto = "15 - Cheque certif.", SatCodigo = "02" },
        new() { Codigo = "22", Texto = "22 - Transferencia", SatCodigo = "03" }
    };
            cboFormaPago.SelectedValue = "10";

            // Uso CFDI (algunos comunes)
            cboUsoCFDI.DisplayMember = "Texto";
            cboUsoCFDI.ValueMember = "Codigo";
            cboUsoCFDI.DataSource = new List<ComboItem>
    {
        new() { Codigo = "G01", Texto = "G01 - Adquisición de mercancías" },
        new() { Codigo = "G03", Texto = "G03 - Gastos en general" },
        new() { Codigo = "S01", Texto = "S01 - Sin efectos fiscales" },
    };
            cboUsoCFDI.SelectedValue = "G03";

            if (cboRegFiscalCfdi != null)
            {
                cboRegFiscalCfdi.DisplayMember = "Texto";
                cboRegFiscalCfdi.ValueMember = "Codigo";
                cboRegFiscalCfdi.DataSource = new List<ComboItem>
                {
                    new() { Codigo = "605", Texto = "605 - Sueldos y Salarios e Ingresos Asimilados a Salarios" },
                    new() { Codigo = "606", Texto = "606 - Arrendamiento" },
                    new() { Codigo = "607", Texto = "607 - Régimen de Enajenación o Adquisición de Bienes" },
                    new() { Codigo = "608", Texto = "608 - Demás ingresos" },
                    new() { Codigo = "610", Texto = "610 - Residentes en el Extranjero sin Establecimiento Permanente" },
                    new() { Codigo = "611", Texto = "611 - Ingresos por Dividendos (socios y accionistas)" },
                    new() { Codigo = "612", Texto = "612 - Personas Físicas con Actividades Empresariales y Profesionales" },
                    new() { Codigo = "614", Texto = "614 - Ingresos por intereses" },
                    new() { Codigo = "615", Texto = "615 - Régimen de los ingresos por obtención de premios" },
                    new() { Codigo = "616", Texto = "616 - Sin obligaciones fiscales" },
                    new() { Codigo = "621", Texto = "621 - Incorporación Fiscal" },
                    new() { Codigo = "625", Texto = "625 - Régimen de las Actividades Empresariales con ingresos a través de Plataformas" },
                    new() { Codigo = "626", Texto = "626 - Régimen Simplificado de Confianza" },
                };
                cboRegFiscalCfdi.SelectedIndex = -1;
            }

            // Estado inicial UI
            txtImporteRecibido.Text = "0.00";
            lblCambio.Text = "Cambio: $0.00";
            lblResumenCobro.Text = FormatearResumenFiscal(0m, 0m, 0m);

            // Si no marcaron "Facturar ahora", deshabilita campos de factura
            chkFacturarAhora.CheckedChanged += (s, e) =>
            {
                bool on = chkFacturarAhora.Checked;
                txtRFC.Enabled = on;
                txtRazon.Enabled = on;
                if (txtCodigoPostalCfdi != null) txtCodigoPostalCfdi.Enabled = on;
                if (cboRegFiscalCfdi != null) cboRegFiscalCfdi.Enabled = on;
                cboUsoCFDI.Enabled = on;
                cboMetodoPago.Enabled = on;
                // Nota: cboFormaPago se usa SIEMPRE para la cobranza, no solo para CFDI
            };
            chkFacturarAhora.Checked = false;
            txtRFC.Enabled = txtRazon.Enabled = cboUsoCFDI.Enabled = cboMetodoPago.Enabled = false;
            if (txtCodigoPostalCfdi != null) txtCodigoPostalCfdi.Enabled = false;
            if (cboRegFiscalCfdi != null) cboRegFiscalCfdi.Enabled = false;

            EnsureCobroMesaSelector();
            RecargarMesasCobroSelector(_mesaSeleccionada?.Id);
            ActualizarVisibilidadCamposCobro();
        }




        private void CboFormaPago_SelectedIndexChanged(object? sender, EventArgs e)
        {
            bool esEfectivo = FormaPagoEsEfectivo();
            txtImporteRecibido.Enabled = esEfectivo;

            if (!esEfectivo)
                txtImporteRecibido.Text = "0.00"; // no se usa para formas no efectivas
            else if (_totalCobroActual > 0m && string.IsNullOrWhiteSpace(txtImporteRecibido.Text))
                txtImporteRecibido.Text = _totalCobroActual.ToString("0.00");

            ActualizarVisibilidadCamposCobro();
            Cobro_RecalcularCambio_UI(null, EventArgs.Empty);
        }

        private void Cobro_RecalcularCambio_UI(object? sender, EventArgs e)
        {
            decimal recibido = 0m;
            decimal.TryParse(txtImporteRecibido.Text, out recibido);

            var forma = GetFormaPagoSatSeleccionada();
            decimal cambio = forma == "01"
                ? Math.Max(0m, Math.Round(recibido - _totalCobroActual, 2))
                : 0m;

            lblCambio.Text = $"Cambio: ${cambio:N2}";
        }

        private void ActualizarVisibilidadCamposCobro()
        {
            var forma = GetFormaPagoSatSeleccionada();
            bool esEfectivo = FormaPagoEsEfectivo();
            bool requiereReferencia = FormaPagoRequiereReferencia();

            Control? FindCobroControl(string name)
            {
                var found = tabCobro.Controls.Find(name, true);
                return found.Length > 0 ? found[0] : null;
            }

            void SetVisible(string name, bool visible)
            {
                var c = FindCobroControl(name);
                if (c != null) c.Visible = visible;
            }

            txtImporteRecibido.Visible = esEfectivo;
            lblCambio.Visible = esEfectivo;
            txtCobroRef.Visible = requiereReferencia;
            txtCobroRef.Enabled = requiereReferencia;
            if (!requiereReferencia)
                txtCobroRef.Text = string.Empty;

            SetVisible("lblRecibidoCobro", esEfectivo);
            SetVisible("lblReferenciaCobro", requiereReferencia);

            var info = FindCobroControl("lblModoCobroInfo") as Label;
            if (info != null)
            {
                if (esEfectivo)
                    info.Text = "En efectivo se calcula el cambio automáticamente.";
                else if (requiereReferencia)
                    info.Text = "Captura la referencia del movimiento si aplica.";
                else
                    info.Text = "El total se cobrará por el medio seleccionado.";
            }

            txtCobroEfectivo.Visible = false;
            txtCobroTarjeta.Visible = false;
            lblCobroCambio.Visible = false;
            btnCobroConfirmar.Visible = false;
        }



        private void btnCfgRecetas_Click(object sender, EventArgs e)
        {
            using (var f = new FormRecetaEditor())
            {
                f.ShowDialog(this);
                if (f.DataChanged)
                    RefrescarCatalogosEnPantalla(f.ChangedCveArt, f.ChangedPrecioPublico);
            }
        }





        private void dgvPedido_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            RecalcularTotales();
        }


        //no se usa esto (y no borrar, si no, explota el programa)
        private void txtRutaAux_TextChanged(object sender, EventArgs e)
        {
        }
        private void dgvInvCaptura_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }
        private void Form1_Load_1(object sender, EventArgs e)
        {
        }
        private void btnAgregarLinea_Click(object sender, EventArgs e)
        {
        }
        private void btnIrCobro_Click(object sender, EventArgs e)
        {
        }
        //hasta aqui lo que no se usa
    }///fin public partial class Form1 : Form

    internal sealed class SimpleTextPromptForm : Form
    {
        private readonly TextBox _txt = new();
        public string Value => _txt.Text;

        public SimpleTextPromptForm(string title, string label, string initial)
        {
            Text = title;
            AppIcon.Apply(this);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 170);

            var lbl = new Label { Text = label, Dock = DockStyle.Top, Height = 40, AutoEllipsis = true };
            _txt.Dock = DockStyle.Top;
            _txt.Margin = new Padding(0, 8, 0, 0);
            _txt.Text = initial ?? string.Empty;

            var btnOk = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 110, Height = 36 };
            var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 110, Height = 36 };
            var fl = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            fl.Controls.Add(btnCancel);
            fl.Controls.Add(btnOk);

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            pnl.Controls.Add(_txt);
            pnl.Controls.Add(lbl);
            Controls.Add(pnl);
            Controls.Add(fl);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}///fin namespace
