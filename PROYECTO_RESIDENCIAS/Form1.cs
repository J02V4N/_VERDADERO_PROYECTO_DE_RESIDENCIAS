using FirebirdSql.Data.FirebirdClient;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Text.RegularExpressions;

namespace PROYECTO_RESIDENCIAS  ///inicio namespace
{
    public partial class Form1 : Form   ///inicio public partial class Form1 : Form
    {

        private FbConnection _saeConn; ///llamada a la conexion de bd


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
            public override string ToString() => $"{Nombre} {(RequierePeso ? $"(${PrecioUnit}/kg)" : $"${PrecioUnit}")}";
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
        }

        public class Pedido
        {
            public int Id { get; set; }
            public int MesaId { get; set; }
            public int MeseroId { get; set; }
            public BindingList<PedidoDet> Detalles { get; set; } = new BindingList<PedidoDet>();
            public bool FacturarAhora { get; set; }
            public decimal Subtotal => Math.Round(Detalles.Sum(d => d.Importe), 2);
            public decimal Impuesto => Math.Round(Subtotal * 0.16m, 2);
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

        public Form1()
        {



            InitializeComponent();

            ResetContextoMesaPedido();
            this.Load += Form1_Load; // <-- SUSCRIBIR
            // Config inicial del timer de “báscula”
            _timerBascula.Interval = 800; // ms
            _timerBascula.Tick += (s, e) => SimularLecturaBascula();

            // IDs del contexto actual (turno/mesa/pedido) cuando una mesa está abierta


            // (opcional) helper para limpiar el contexto


            this.Shown += (s, e) => EnsureMesaSeleccionada();

            



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
                DataPropertyName = "Cantidad",
                HeaderText = "Cant x 1",
                Width = 80,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N3", Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            dgvReceta.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Existencia",
                HeaderText = "Exist.",
                Width = 80,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N3", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvReceta.AllowUserToAddRows = false;
            dgvReceta.AllowUserToDeleteRows = false;
            dgvReceta.MultiSelect = false;
            dgvReceta.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvReceta.DataSource = _recetaActual;
        }



        private void Form1_Load(object sender, EventArgs e)
        {


            // 1) Carga de catálogos 

            CargarMeserosDesdeAux();
            CargarMesasDesdeAux();
            CargarPlatillosDesdeSae_ListBox();


            // 2) Bindings
            ConfigurarGrids();
            dgvMesas.DataSource = _mesas;
            cboMesero.DataSource = _meseros;
            cboMesero.DisplayMember = "Nombre";
            cboMesero.ValueMember = "Id";
            lbPlatillos.DataSource = _platillos;

            // 3) Eventos de UI
            dgvMesas.SelectionChanged += (s, ev) => SeleccionarMesa();
            dgvMesas.SelectionChanged += (s, ev) => EnsureMesaSeleccionada();

            dgvMesas.SelectionChanged += dgvMesas_SelectionChanged;



            lbPlatillos.DoubleClick += (s, ev) => AgregarPlatilloSeleccionado();
            btnAgregarLinea.Click += (s, ev) => AgregarPlatilloSeleccionado();
            //chkSimularBascula.CheckedChanged += (s, e) => UpdateScaleTimer();

            btnIrCobro.Click += (s, ev) => IrACobro();
            chkFacturarAhora.CheckedChanged += (s, ev) => ToggleCamposFactura();
            btnConfirmarCobro.Click += (s, ev) => ConfirmarCobro();

            // Hotkeys una sola vez:
            lbInvArticulos.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AgregarEntradaInventario(); e.Handled = true; } };
            dgvInvCaptura.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { EliminarLineaInventario(); e.Handled = true; } };

            CargarImpresoras();
            ActualizarUI();

            this.KeyPreview = true; // para atajos




            UpdateStatus("SAE", true); // si ya probaste OK
            UpdateStatus("AUX", true);
            UpdateStatus("BAS", _timerBascula.Enabled);


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
            btnInvRefrescar.Click += (s, e) => CargarInvArticulosDesdeSAE();
            //chkInvSimularBascula.CheckedChanged += (s, e) => UpdateScaleTimer();
            btnInvAgregar.Click += (s, e) => AgregarEntradaInventario();
            btnInvGuardarAux.Click += (s, e) => GuardarEntradasInventarioEnAux();
            btnInvLimpiar.Click += (s, e) => LimpiarCapturaInventario();

            // Config grilla de inventario
            dgvInvCaptura.AutoGenerateColumns = false;
            dgvInvCaptura.Columns.Clear();
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = "Partida", Width = 40, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Clave", DataPropertyName = "Clave", Width = 100, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripción", DataPropertyName = "Nombre", Width = 220, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Peso (g)", DataPropertyName = "PesoGr", Width = 80 });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kg", DataPropertyName = "PesoKg", Width = 70, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Costo/Kg", DataPropertyName = "CostoKg", Width = 80 });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Importe", DataPropertyName = "Importe", Width = 80, ReadOnly = true });
            dgvInvCaptura.DataSource = _invEntradas;
            dgvInvCaptura.CellEndEdit += (s, e) => RecalcularTotalesInventario();

            lbPlatillos.SelectedIndexChanged += lbPlatillos_SelectedIndexChanged;


            txtCobroEfectivo.TextChanged += Cobro_RecalcularCambio;
            txtCobroTarjeta.TextChanged += Cobro_RecalcularCambio;




            btnCobroConfirmar.Click += btnCobroConfirmar_Click;
            btnCobroCancelar.Click += (s, e) => { tabMain.SelectedTab = tabPedido; };



            InicializarTabCobro();

            txtImporteRecibido.TextChanged += Cobro_RecalcularCambio_UI;
            cboFormaPago.SelectedIndexChanged += CboFormaPago_SelectedIndexChanged;

            btnConfirmarCobro.Click += btnConfirmarCobro_Click;
            btnReimprimir.Click += (s, e) => MessageBox.Show("Reimpresión pendiente (lo conectamos cuando definamos el formato).");




            // Doble-click en la lista de platillos => agregar
            //lbPlatillos.DoubleClick += (s, e) => AgregarPlatilloSeleccionado();



            // Doble-click en la grilla del pedido => quitar línea
            dgvPedido.CellDoubleClick += dgvPedido_CellDoubleClick;

            // (si agregas el botón "Quitar" en el diseñador con Name=btnQuitarLinea)
            btnQuitarLinea.Click += btnQuitarLinea_Click;


            // Carga inicial del catálogo desde SAE (si quieres al abrir)
            CargarInvArticulosDesdeSAE();


            ConfigurarGridReceta();



        }


        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        private void UpdateStatus(string who, bool ok)
        {
            if (who == "SAE") tslSae.Text = ok ? "SAE: Conectado" : "SAE: OFF";
            if (who == "AUX") tslAux.Text = ok ? "Aux: Conectada" : "Aux: OFF";
            if (who == "BAS") tslBascula.Text = ok ? "Báscula: ON" : "Báscula: OFF";
        }
        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

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
            var colMesero = dgvMesas.Columns.Cast<DataGridViewColumn>()
                  .FirstOrDefault(c => c.HeaderText.Equals("Mesero", StringComparison.OrdinalIgnoreCase));
            if (colMesero != null) colMesero.DataPropertyName = "MeseroNombre";


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

                var rows = SaeDb.ListarPlatillos(listaPrecio: lista, almacen: alm);

                // Mapea a tu clase de pedido (la ListBox usa el .ToString() que ya definiste)
                _platillos = rows.Select(r => new Platillo
                {
                    Clave = r.Clave,
                    Nombre = r.Descripcion,
                    PrecioUnit = r.Precio,       // $ por pieza (si luego quieres manejar $/kg, aquí lo adaptamos)
                    RequierePeso = false           // por ahora en false; luego lo atamos a una bandera/INSUMO_EXT
                }).ToList();

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
                DataPropertyName = "MeseroId",
                Width = 110,
                ReadOnly = true
            });

            dgvMesas.CellFormatting -= DgvMesas_CellFormatting;
            dgvMesas.CellFormatting += DgvMesas_CellFormatting;

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
                HeaderText = "P.Unit",
                DataPropertyName = "PrecioUnit",
                Width = 70,
                ReadOnly = true
            });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colImporte",
                HeaderText = "Importe",
                DataPropertyName = "Importe",
                Width = 80,
                ReadOnly = true
            });

            // Formatos (usan Name, ya no HeaderText)
            dgvPedido.Columns["colPrecioUnit"].DefaultCellStyle.Format = "N2";
            dgvPedido.Columns["colImporte"].DefaultCellStyle.Format = "N2";
            dgvPedido.Columns["colPesoGr"].DefaultCellStyle.Format = "N0";
            dgvPedido.Columns["colCantidad"].DefaultCellStyle.Format = "N2";

            dgvPedido.CellEndEdit -= (s, e) => RecalcularTotales();
            dgvPedido.CellEndEdit += (s, e) => RecalcularTotales();

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

        private void DgvMesas_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvMesas.Columns[e.ColumnIndex].Name == "colMesaMesero" && e.Value is int id && id > 0)
            {
                var m = _meseros.FirstOrDefault(x => x.Id == id);
                e.Value = m?.Nombre ?? "";
            }
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
                    _pedidoActual = null;
                    dgvPedido.DataSource = null;
                    lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";
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
                    _pedidoActual = null;
                    dgvPedido.DataSource = null;
                    lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";
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

                AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value);
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
                var (sub, imp, tot) = AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value);
                lblTotales.Text = $"Subtotal: ${sub:N2}   IVA: ${imp:N2}   Total: ${tot:N2}";
            }
            ActualizarTotales();
        }

        private void ActualizarTotales()
        {
            if (_pedidoActual == null)
            {
                lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";
                return;
            }
            lblTotales.Text = $"Subtotal: ${_pedidoActual.Subtotal:N2}   IVA: ${_pedidoActual.Impuesto:N2}   Total: ${_pedidoActual.Total:N2}";
        }

        private void IrACobro()
        {
            if (_idPedidoActual == null)
            {
                MessageBox.Show("No hay pedido abierto. Abre la mesa primero.", "Pedido",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Totales desde BD
            var (sub, imp, tot) = AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value);
            _totalCobroActual = tot;

            // UI: resumen y valores por defecto
            lblResumenCobro.Text = $"Subtotal: ${sub:N2}   IVA: ${imp:N2}   Total: ${tot:N2}";
            cboFormaPago.SelectedValue = "01";          // Efectivo por defecto
            txtImporteRecibido.Enabled = true;
            txtImporteRecibido.Text = tot.ToString("0.00");
            Cobro_RecalcularCambio_UI(null, EventArgs.Empty);

            tabMain.SelectedTab = tabCobro;
            txtImporteRecibido.Focus();
            txtImporteRecibido.SelectAll();
        }



        private void ToggleCamposFactura()
        {

            bool on = chkFacturarAhora.Checked;
            txtRFC.Enabled = txtRazon.Enabled = cboUsoCFDI.Enabled = on;
            // También podrías forzar validaciones cuando on=true
        }

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
            lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";

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
                lblInvKg.Text = $"{(gramos / 1000m):N3} kg";
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

                // Verifica CONFIG y USUARIOS (admin sembrado)
                int cfgCount, adminCount;
                using (var cmd = new FbCommand("SELECT COUNT(*) FROM CONFIG", auxConn))
                    cfgCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new FbCommand("SELECT COUNT(*) FROM USUARIOS WHERE USERNAME='admin'", auxConn))
                    adminCount = Convert.ToInt32(cmd.ExecuteScalar());

                MessageBox.Show(
                    "BD Auxiliar OK\n" +
                    $"Ruta: {auxPath}\n" +
                    $"Tablas usuario: {tablasUsuario}\n" +
                    $"CONFIG rows: {cfgCount}\n" +
                    $"Usuarios admin: {adminCount}",
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
            public decimal? FacConv { get; set; } // kg<->g si aplica
            public override string ToString() => $"{Clave} - {Descr}";
        }

        public class EntradaInvSession
        {
            public int Partida { get; set; }
            public string Clave { get; set; }
            public string Nombre { get; set; }
            public decimal PesoGr { get; set; }
            public decimal PesoKg => Math.Round(PesoGr / 1000m, 3);
            public decimal CostoKg { get; set; } // opcional
            public decimal Importe => Math.Round(PesoKg * CostoKg, 2);
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




                var clie = SaeDb.GetTableName(sae, "CLIE");
                var prov = SaeDb.GetTableName(sae, "PROV");
                var kits = SaeDb.GetTableName(sae, "KITS");

                var invTable = SaeDb.GetTableName(sae, "INVE");
                using var cmd = new FbCommand($@"
SELECT FIRST 500
       CVE_ART, DESCR, UNI_MED, UNI_ALT, FAC_CONV
FROM {invTable}
ORDER BY CVE_ART", sae);

                using var rd = cmd.ExecuteReader();
                var list = new List<InvArticulo>();
                while (rd.Read())
                {
                    list.Add(new InvArticulo
                    {
                        Clave = rd["CVE_ART"]?.ToString()?.Trim(),
                        Descr = rd["DESCR"]?.ToString()?.Trim(),
                        UniMed = rd["UNI_MED"]?.ToString()?.Trim(),
                        UniAlt = rd["UNI_ALT"]?.ToString()?.Trim(),
                        FacConv = rd["FAC_CONV"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(rd["FAC_CONV"])
                    });
                }
                _invArticulos = list;
                lbInvArticulos.DataSource = _invArticulos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible leer INVE01 de SAE.\n" + ex.Message, "Inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // fallback: deja la lista vacía o con dummy
                _invArticulos = new List<InvArticulo>();
                lbInvArticulos.DataSource = _invArticulos;
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
            if (!decimal.TryParse(txtInvPesoGr.Text, out var gramos) || gramos <= 0)
            {
                MessageBox.Show("Lectura de peso inválida.");
                return;
            }

            decimal costoKg = 0m;
            decimal.TryParse(txtInvCostoKg.Text, out costoKg);

            var det = new EntradaInvSession
            {
                Partida = _invEntradas.Count + 1,
                Clave = art.Clave,
                Nombre = art.Descr,
                PesoGr = gramos,
                CostoKg = costoKg
            };
            _invEntradas.Add(det);
            RecalcularTotalesInventario();

            // limpia lectura
            // (no borramos costo para que se repita si capturas varias entradas)
        }

        private void RecalcularTotalesInventario()
        {
            dgvInvCaptura.EndEdit();
            decimal kg = _invEntradas.Sum(x => x.PesoKg);
            decimal imp = _invEntradas.Sum(x => x.Importe);
            lblInvTotales.Text = $"Entradas: {_invEntradas.Count}   Kg: {kg:N3}   $: {imp:N2}";
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


        private void GuardarEntradasInventarioEnAux()
        {
            if (_invEntradas.Count == 0)
            {
                MessageBox.Show("No hay entradas para guardar.");
                return;
            }

            try
            {
                string auxPath;
                using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
                AuxDbInitializer.EnsureMovInvAux(aux);

                using var tx = aux.BeginTransaction();
                foreach (var e in _invEntradas)
                {
                    using var cmd = new FbCommand(@"
INSERT INTO MOV_INV_AUX
(CLAVE_ARTICULO_SAE, PESO_GR, COSTO_KG, IMPORTE, ORIGEN, TIPO, POST_SAE)
VALUES (@CVE, @GR, @CKG, @IMP, 'BASCULA', 'ENTRADA', 0)", aux, tx);

                    cmd.Parameters.Add(new FbParameter("@CVE", FbDbType.VarChar, 30) { Value = e.Clave });
                    cmd.Parameters.Add(new FbParameter("@GR", FbDbType.Decimal) { Value = e.PesoGr, Precision = 18, Scale = 6 });
                    cmd.Parameters.Add(new FbParameter("@CKG", FbDbType.Decimal) { Value = e.CostoKg, Precision = 18, Scale = 6 });
                    cmd.Parameters.Add(new FbParameter("@IMP", FbDbType.Decimal) { Value = e.Importe, Precision = 18, Scale = 6 });
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();

                MessageBox.Show($"Guardadas {_invEntradas.Count} entradas en Aux.\n" +
                                "Quedan pendientes de enviar a SAE.", "Inventario",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                _invEntradas.Clear();
                RecalcularTotalesInventario();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error guardando en Aux:\n" + ex.Message, "Inventario",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
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
            if (_pedidoActual == null) return;
            decimal recibido = 0m;
            decimal.TryParse(txtImporteRecibido.Text, out recibido);
            decimal cambio = Math.Max(0, recibido - _pedidoActual.Total);
            lblCambio.Text = $"Cambio: ${cambio:N2}";
        }


        private void CargarImpresoras()
        {
            cboImpresora.Items.Clear();
            foreach (string p in PrinterSettings.InstalledPrinters)
                cboImpresora.Items.Add(p);
        }

       // private void UpdateScaleTimer()
       // {
       //     _timerBascula.Enabled = chkSimularBascula.Checked || chkInvSimularBascula.Checked;
        //}


        private bool ValidarCobro()
        {
            if (_pedidoActual == null || _pedidoActual.Detalles.Count == 0) { MessageBox.Show("No hay partidas."); return false; }

            if (chkFacturarAhora.Checked)
            {
                if (!Regex.IsMatch(txtRFC.Text.Trim().ToUpperInvariant(), @"^([A-ZÑ&]{3,4})\d{6}[A-Z0-9]{3}$"))
                { MessageBox.Show("RFC inválido."); return false; }
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

            txtPuertoCom.Text = Get("BASCULA_PUERTO");
            var imp = Get("IMPRESORA_TICKET");
            if (!string.IsNullOrEmpty(imp) && cboImpresora.Items.Contains(imp)) cboImpresora.SelectedItem = imp;
            cboAlmacen.Text = Get("ALMACEN_DEFAULT");
            cboListaPrecios.Text = Get("LISTA_PRECIOS");
        }

        private void GuardarConfigUI()
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            AuxDbInitializer.UpsertConfig(aux, "BASCULA_PUERTO", txtPuertoCom.Text);
            AuxDbInitializer.UpsertConfig(aux, "IMPRESORA_TICKET", cboImpresora.Text);
            AuxDbInitializer.UpsertConfig(aux, "ALMACEN_DEFAULT", cboAlmacen.Text);
            AuxDbInitializer.UpsertConfig(aux, "LISTA_PRECIOS", cboListaPrecios.Text);
        }


        

        private void btnConfirmarCobro_Click(object? sender, EventArgs e)
        {
            if (_idPedidoActual == null)
            {
                MessageBox.Show("No hay pedido abierto.", "Cobro",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var forma = (cboFormaPago.SelectedValue as string) ?? "01";
            var metodo = (cboMetodoPago.SelectedValue as string) ?? "PUE";
            var uso = (cboUsoCFDI.SelectedValue as string) ?? "G03";

            decimal recibido = 0m;
            decimal.TryParse(txtImporteRecibido.Text, out recibido);

            decimal eff = 0m, tar = 0m;

            if (forma == "01") // EFECTIVO
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

            // Si van a facturar ahora, valida RFC y Razón (no timbramos aún; solo dejamos listo)
            if (chkFacturarAhora.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtRFC.Text) || string.IsNullOrWhiteSpace(txtRazon.Text))
                {
                    MessageBox.Show("Captura RFC y Razón social para facturar.", "CFDI",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Aquí, más adelante: guardar estos datos y timbrar.
                // RFC: txtRfc.Text.Trim()
                // Razon: txtRazon.Text.Trim()
                // Metodo pago CFDI: metodo (PUE/PPD)
                // Forma pago CFDI: forma (01/03/04/28/99)
                // Uso CFDI: uso (G03, etc.)
            }

            try
            {
                var res = AuxRepo.CobrarPedido(_idPedidoActual.Value, eff, tar, referenciaTarjeta: null);

                // Actualiza estado de la mesa en UI
                if (_mesaSeleccionada != null)
                {
                    _mesaSeleccionada.Estado = MesaEstado.EN_CUENTA;
                    RefrescarFilaMesaActual();
                    lblMesaSel.Text = $"Seleccionada: {_mesaSeleccionada.Nombre} ({_mesaSeleccionada.Estado})";
                }

                // Limpia pedido y UI de pedido
                _pedidoActual = null;
                _idPedidoActual = null;
                dgvPedido.DataSource = null;
                lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";

                // Resumen
                var cambioTxt = lblCambio.Text;
                var resumen = $"Total: ${res.Total:N2}\n" +
                              $"Forma: {((ComboItem)cboFormaPago.SelectedItem).Texto}\n" +
                              (forma == "01" ? $"Recibido: ${recibido:N2}\n{cambioTxt}" : "") +
                              (chkFacturarAhora.Checked ? $"\nCFDI: RFC {txtRFC.Text.Trim()}  Uso {uso}  Método {metodo}" : "");

                MessageBox.Show("Cobro realizado.\n\n" + resumen, "Cobro",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Regresa a Mesas
                tabMain.SelectedTab = tabMesas;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private void btnGuardarConfig_Click(object sender, EventArgs e)
        {

            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            AuxDbInitializer.UpsertConfig(aux, "IMPRESORA_TICKET", cboImpresora.Text);
            AuxDbInitializer.UpsertConfig(aux, "BASCULA_PUERTO", txtPuertoCom.Text);
            AuxDbInitializer.UpsertConfig(aux, "ALMACEN_DEFAULT", cboAlmacen.Text);
            AuxDbInitializer.UpsertConfig(aux, "LISTA_PRECIOS", cboListaPrecios.Text);
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
            lbInvArticulos.DoubleClick += (s, e) => AgregarEntradaInventario();
        }


        // Botón "Aplicar costo a todos"
        private void btnInvAplicarCostoTodos_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtInvCostoKg.Text, out var c) || c < 0) { MessageBox.Show("Costo inválido."); return; }
            foreach (var it in _invEntradas) it.CostoKg = c;
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
                    throw new Exception("CONFIG.SAE_FDB está vacío. Selecciona la empresa desde el selector.");
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

                // Refleja en UI
                mesa.Estado = MesaEstado.LIBRE;
                mesa.MeseroId = null;
                mesa.MeseroNombre = null;
                dgvMesas.Refresh();

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
                _pedidoActual = null;
                dgvPedido.DataSource = null;
                lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";
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
            var (sub, imp, tot) = AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value);
            lblTotales.Text = $"Subtotal: ${sub:N2}   IVA: ${imp:N2}   Total: ${tot:N2}";
        }





        private void QuitarLineaSeleccionada()
        {
            if (dgvPedido.CurrentRow?.DataBoundItem is not PedidoDet sel) return;

            // Persistido en BD
            if (_idPedidoActual != null && sel.IdDet.HasValue)
            {
                AuxRepo.EliminarPedidoDet(sel.IdDet.Value);
                AuxRepo.RecalcularTotalesPedido(_idPedidoActual.Value);
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
            decimal total = 0m, eff = 0m, tar = 0m;
            // tomar total del label
            var sTot = lblCobroTotal.Text.Replace("Total:", "").Replace("$", "").Trim();
            decimal.TryParse(sTot, out total);
            decimal.TryParse(txtCobroEfectivo.Text, out eff);
            decimal.TryParse(txtCobroTarjeta.Text, out tar);

            var cambio = Math.Max(0m, Math.Round((eff + tar) - total, 2));
            lblCobroCambio.Text = $"Cambio: ${cambio:N2}";
        }



        private void btnCobroConfirmar_Click(object? sender, EventArgs e)
        {
            if (_idPedidoActual == null)
            {
                MessageBox.Show("No hay pedido abierto.", "Cobro",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal eff = 0m, tar = 0m;
            decimal.TryParse(txtCobroEfectivo.Text, out eff);
            decimal.TryParse(txtCobroTarjeta.Text, out tar);
            string refTar = string.IsNullOrWhiteSpace(txtCobroRef.Text) ? null : txtCobroRef.Text.Trim();

            try
            {
                var res = AuxRepo.CobrarPedido(_idPedidoActual.Value, eff, tar, refTar);

                // UI: mesa pasa a EN_CUENTA
                if (_mesaSeleccionada != null)
                {
                    _mesaSeleccionada.Estado = MesaEstado.EN_CUENTA;
                    RefrescarFilaMesaActual();
                    lblMesaSel.Text = $"Seleccionada: {_mesaSeleccionada.Nombre} ({_mesaSeleccionada.Estado})";
                }

                // Limpia pedido actual en UI
                _pedidoActual = null;
                _idPedidoActual = null;
                dgvPedido.DataSource = null;
                lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";

                // Mensaje y regreso a Mesas
                MessageBox.Show($"Cobro realizado.\nTotal: ${res.Total:N2}\nPagado: ${res.Pagado:N2}\nCambio: ${res.Cambio:N2}",
                                "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Information);

                tabMain.SelectedTab = tabMesas;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo cobrar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }




        private decimal _totalCobroActual = 0m;

        private class ComboItem
        {
            public string Codigo { get; set; }
            public string Texto { get; set; }
            public override string ToString() => Texto;
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

            // Forma de pago (CFDI): 01 Efectivo, 03 Transferencia, 04 TC, 28 TD, 99 Por definir
            cboFormaPago.DisplayMember = "Texto";
            cboFormaPago.ValueMember = "Codigo";
            cboFormaPago.DataSource = new List<ComboItem>
    {
        new() { Codigo = "01", Texto = "01 - Efectivo" },
        new() { Codigo = "03", Texto = "03 - Transferencia electrónica" },
        new() { Codigo = "04", Texto = "04 - Tarjeta de crédito" },
        new() { Codigo = "28", Texto = "28 - Tarjeta de débito" },
        new() { Codigo = "99", Texto = "99 - Por definir" }
    };
            cboFormaPago.SelectedValue = "01";

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

            // Estado inicial UI
            txtImporteRecibido.Text = "0.00";
            lblCambio.Text = "Cambio: $0.00";
            lblResumenCobro.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";

            // Si no marcaron "Facturar ahora", deshabilita campos de factura
            chkFacturarAhora.CheckedChanged += (s, e) =>
            {
                bool on = chkFacturarAhora.Checked;
                txtRFC.Enabled = on;
                txtRazon.Enabled = on;
                cboUsoCFDI.Enabled = on;
                cboMetodoPago.Enabled = on;
                // Nota: cboFormaPago se usa SIEMPRE para la cobranza, no solo para CFDI
            };
            chkFacturarAhora.Checked = false;
            txtRFC.Enabled = txtRazon.Enabled = cboUsoCFDI.Enabled = cboMetodoPago.Enabled = false;
        }




        private void CboFormaPago_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var forma = (cboFormaPago.SelectedValue as string) ?? "01";
            bool esEfectivo = forma == "01";
            txtImporteRecibido.Enabled = esEfectivo;

            if (!esEfectivo)
                txtImporteRecibido.Text = "0.00"; // no se usa para tarjeta/transferencia

            Cobro_RecalcularCambio_UI(null, EventArgs.Empty);
        }

        private void Cobro_RecalcularCambio_UI(object? sender, EventArgs e)
        {
            decimal recibido = 0m;
            decimal.TryParse(txtImporteRecibido.Text, out recibido);

            var forma = (cboFormaPago.SelectedValue as string) ?? "01";
            decimal cambio = forma == "01"
                ? Math.Max(0m, Math.Round(recibido - _totalCobroActual, 2))
                : 0m;

            lblCambio.Text = $"Cambio: ${cambio:N2}";
        }



        

        //no se usa esto (y no borrar, si no, explota el programa)

        private void txtRutaAux_TextChanged(object sender, EventArgs e)
        {

        }

        private void dgvInvCaptura_CellContentClick(object sender, EventArgs e)
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
}///fin namespace
