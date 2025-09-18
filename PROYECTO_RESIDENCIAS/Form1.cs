using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Printing;

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
            public decimal PrecioUnit { get; set; }    // $ por pieza o por kg (seg�n RequierePeso)
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
            this.Load += Form1_Load; // <-- SUSCRIBIR
            // Config inicial del timer de �b�scula�
            _timerBascula.Interval = 800; // ms
            _timerBascula.Tick += (s, e) => SimularLecturaBascula();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            

            // 1) Carga de cat�logos dummy
            SeedMeseros();
            SeedMesas(12);
            SeedPlatillos();

            // 2) Bindings
            ConfigurarGrids();
            dgvMesas.DataSource = _mesas;
            cboMesero.DataSource = _meseros;
            cboMesero.DisplayMember = "Nombre";
            cboMesero.ValueMember = "Id";
            lbPlatillos.DataSource = _platillos;

            // 3) Eventos de UI
            dgvMesas.SelectionChanged += (s, ev) => SeleccionarMesa();
            btnAbrirMesa.Click += (s, ev) => AbrirAtenderMesa();
            lbPlatillos.DoubleClick += (s, ev) => AgregarPlatilloSeleccionado();
            btnAgregarLinea.Click += (s, ev) => AgregarPlatilloSeleccionado();
            chkSimularBascula.CheckedChanged += (s, e) => UpdateScaleTimer();
            
            btnIrCobro.Click += (s, ev) => IrACobro();
            chkFacturarAhora.CheckedChanged += (s, ev) => ToggleCamposFactura();
            btnConfirmarCobro.Click += (s, ev) => ConfirmarCobro();

            // Hotkeys una sola vez:
            lbInvArticulos.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { AgregarEntradaInventario(); e.Handled = true; } };
            dgvInvCaptura.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { EliminarLineaInventario(); e.Handled = true; } };

            CargarImpresoras();
            ActualizarUI();

            this.KeyPreview = true; // para atajos



            //var status = new StatusStrip();
            //var sSae = new ToolStripStatusLabel("SAE: ?");
            //var sAux = new ToolStripStatusLabel("Aux: ?");
            //var sBas = new ToolStripStatusLabel("B�scula: OFF");
            //status.Items.AddRange(new ToolStripItem[] { sSae, sAux, sBas });
            //status.SizingGrip = false;
            //this.Controls.Add(status);



            // Guarda referencias en campos privados
            //tslSae = sSae; tslAux = sAux; tslBascula = sBas;
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
            chkInvSimularBascula.CheckedChanged += (s, e) => UpdateScaleTimer();
            btnInvAgregar.Click += (s, e) => AgregarEntradaInventario();
            btnInvGuardarAux.Click += (s, e) => GuardarEntradasInventarioEnAux();
            btnInvLimpiar.Click += (s, e) => LimpiarCapturaInventario();

            // Config grilla de inventario
            dgvInvCaptura.AutoGenerateColumns = false;
            dgvInvCaptura.Columns.Clear();
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = "Partida", Width = 40, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Clave", DataPropertyName = "Clave", Width = 100, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripci�n", DataPropertyName = "Nombre", Width = 220, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Peso (g)", DataPropertyName = "PesoGr", Width = 80 });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kg", DataPropertyName = "PesoKg", Width = 70, ReadOnly = true });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Costo/Kg", DataPropertyName = "CostoKg", Width = 80 });
            dgvInvCaptura.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Importe", DataPropertyName = "Importe", Width = 80, ReadOnly = true });
            dgvInvCaptura.DataSource = _invEntradas;
            dgvInvCaptura.CellEndEdit += (s, e) => RecalcularTotalesInventario();

            // Carga inicial del cat�logo desde SAE (si quieres al abrir)
            CargarInvArticulosDesdeSAE();


        }


        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        private void UpdateStatus(string who, bool ok)
        {
            if (who == "SAE") tslSae.Text = ok ? "SAE: Conectado" : "SAE: OFF";
            if (who == "AUX") tslAux.Text = ok ? "Aux: Conectada" : "Aux: OFF";
            if (who == "BAS") tslBascula.Text = ok ? "B�scula: ON" : "B�scula: OFF";
        }
        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        // ======== SEED DATA ========

        private void SeedMeseros()
        {
            _meseros = new List<Mesero>
            {
                new Mesero{ Id=1, Nombre="Ana" },
                new Mesero{ Id=2, Nombre="Luis" },
                new Mesero{ Id=3, Nombre="Sof�a" }
            };
        }

        private void SeedMesas(int cantidad)
        {
            _mesas.Clear();
            for (int i = 1; i <= cantidad; i++)
            {
                _mesas.Add(new Mesa
                {
                    Id = i,
                    Nombre = $"Mesa {i}",
                    Capacidad = (i % 4) + 2,
                    Estado = MesaEstado.LIBRE
                });
            }
        }

        private void SeedPlatillos()
        {
            _platillos = new List<Platillo>
            {
                new Platillo{ Clave="TAC-AR", Nombre="Tacos Arrachera", PrecioUnit=35m, RequierePeso=false },
                new Platillo{ Clave="PST-AL", Nombre="Pasta Alfredo", PrecioUnit=89m, RequierePeso=false },
                new Platillo{ Clave="CAR-AL", Nombre="Carne al peso", PrecioUnit=360m, RequierePeso=true }, // $/kg
                new Platillo{ Clave="QSO-FD", Nombre="Queso fundido", PrecioUnit=75m, RequierePeso=false },
                new Platillo{ Clave="CAM-PS", Nombre="Camar�n al peso", PrecioUnit=520m, RequierePeso=true } // $/kg
            };
        }

        // ======== UI / BINDINGS ========

        private void ConfigurarGrids()
        {
            // Mesas
            dgvMesas.AutoGenerateColumns = false;
            dgvMesas.Columns.Clear();
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Id", DataPropertyName = "Id", Width = 40 });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mesa", DataPropertyName = "Nombre", Width = 100 });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cap.", DataPropertyName = "Capacidad", Width = 50 });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Estado", Width = 90 });
            dgvMesas.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Mesero",
                Width = 110,
                DataPropertyName = "MeseroId"
            });
            dgvMesas.CellFormatting += (s, e) =>
            {
                if (dgvMesas.Columns[e.ColumnIndex].HeaderText == "Mesero" && e.Value is int id && id > 0)
                {
                    var m = _meseros.FirstOrDefault(x => x.Id == id);
                    e.Value = m?.Nombre ?? "";
                }
            };

            // Pedido
            dgvPedido.AutoGenerateColumns = false;
            dgvPedido.Columns.Clear();
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = "Partida", Width = 40, ReadOnly = true });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Clave", DataPropertyName = "Clave", Width = 80, ReadOnly = true });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Descripci�n", DataPropertyName = "Nombre", Width = 190, ReadOnly = true });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cant", DataPropertyName = "Cantidad", Width = 60 });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Peso (g)", DataPropertyName = "PesoGr", Width = 80 });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "P.Unit", DataPropertyName = "PrecioUnit", Width = 70, ReadOnly = true });
            dgvPedido.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Importe", DataPropertyName = "Importe", Width = 80, ReadOnly = true });

            dgvPedido.CellEndEdit += (s, e) => RecalcularTotales();
        }

        private void SeleccionarMesa()
        {
            if (dgvMesas.CurrentRow?.DataBoundItem is Mesa m)
            {
                _mesaSeleccionada = m;
                lblMesaSel.Text = $"Seleccionada: {m.Nombre} ({m.Estado})";
                if (_pedidosAbiertos.TryGetValue(m.Id, out var ped))
                {
                    _pedidoActual = ped;
                    dgvPedido.DataSource = ped.Detalles;
                    ActualizarTotales();
                }
                else
                {
                    dgvPedido.DataSource = null;
                    lblTotales.Text = "Subtotal: $0.00   IVA: $0.00   Total: $0.00";
                }
            }
        }

        private void AbrirAtenderMesa()
        {
            if (_mesaSeleccionada == null) return;
            if (cboMesero.SelectedItem is Mesero mesero)
            {
                _mesaSeleccionada.MeseroId = mesero.Id;
            }

            if (!_pedidosAbiertos.ContainsKey(_mesaSeleccionada.Id))
            {
                var ped = new Pedido
                {
                    Id = Environment.TickCount,
                    MesaId = _mesaSeleccionada.Id,
                    MeseroId = _mesaSeleccionada.MeseroId ?? 0
                };
                _pedidosAbiertos[_mesaSeleccionada.Id] = ped;
                _mesaSeleccionada.Estado = MesaEstado.OCUPADA;
            }

            _pedidoActual = _pedidosAbiertos[_mesaSeleccionada.Id];
            dgvPedido.DataSource = _pedidoActual.Detalles;

            tabMain.SelectedTab = tabPedido;
            ActualizarUI();
        }

        private void AgregarPlatilloSeleccionado()
        {
            if (_pedidoActual == null) return;
            if (lbPlatillos.SelectedItem is Platillo p)
            {
                var det = new PedidoDet
                {
                    Partida = _pedidoActual.Detalles.Count + 1,
                    Clave = p.Clave,
                    Nombre = p.Nombre,
                    RequierePeso = p.RequierePeso,
                    Cantidad = p.RequierePeso ? 1 : 1, // si pesa, manejamos PesoGr; Cantidad=1 simb�lica
                    PrecioUnit = p.PrecioUnit
                };

                if (p.RequierePeso)
                {
                    // tomar lo que est� en txtPesoGr (simulado o real)
                    if (decimal.TryParse(txtPesoGr.Text, out var gr) && gr > 0)
                        det.PesoGr = gr;
                    else
                        det.PesoGr = 150m; // default si no hay lectura (ej. 150g)
                }

                _pedidoActual.Detalles.Add(det);
                RecalcularTotales();
            }
        }

        private void RecalcularTotales()
        {
            // Normaliza celdas editables (Cant / Peso) y recalcula
            dgvPedido.EndEdit();
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
            if (_pedidoActual == null || _pedidoActual.Detalles.Count == 0) return;
            lblResumenCobro.Text = $"Mesa: {_mesaSeleccionada?.Nombre}\n" +
                                   $"Mesero: {_meseros.FirstOrDefault(x => x.Id == _pedidoActual.MeseroId)?.Nombre}\n" +
                                   $"Partidas: {_pedidoActual.Detalles.Count}\n" +
                                   $"Subtotal: ${_pedidoActual.Subtotal:N2}\n" +
                                   $"IVA: ${_pedidoActual.Impuesto:N2}\n" +
                                   $"TOTAL: ${_pedidoActual.Total:N2}";

            chkFacturarAhora.Checked = false;
            txtRFC.Enabled = txtRazon.Enabled = cboUsoCFDI.Enabled = false;

            tabMain.SelectedTab = tabCobro;
        }

        private void ToggleCamposFactura()
        {

            bool on = chkFacturarAhora.Checked;
            txtRFC.Enabled = txtRazon.Enabled = cboUsoCFDI.Enabled = on;
            // Tambi�n podr�as forzar validaciones cuando on=true
        }

        private void ConfirmarCobro()
        {
            if (_pedidoActual == null) return;

            _pedidoActual.FacturarAhora = chkFacturarAhora.Checked;
            // Aqu� solo simulamos �cobrado�
            MessageBox.Show(_pedidoActual.FacturarAhora
                ? "Cobro confirmado. (Simulaci�n) Facturar ahora."
                : "Cobro confirmado. (Simulaci�n) Ticket / Facturar despu�s.",
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

        // ======== �B�SCULA� SIMULADA ========

        private readonly Random _rnd = new Random();

        private void SimularLecturaBascula()
        {
            decimal gramos = _rnd.Next(80, 351);

            // Pedido (ya exist�a)
            if (chkSimularBascula.Checked && txtPesoGr != null && !txtPesoGr.IsDisposed)
                txtPesoGr.Text = gramos.ToString("0");

            // Inventario (nuevo)
            if (chkInvSimularBascula.Checked && txtInvPesoGr != null && !txtInvPesoGr.IsDisposed)
            {
                txtInvPesoGr.Text = gramos.ToString("0");
                lblInvKg.Text = $"{(gramos / 1000m):N3} kg";
            }
        }


        private void prueba_conexion_Click(object sender, EventArgs e)
        {

        }


        private void btnPruebaAux_Click(object sender, EventArgs e)
        {
            // 2) BD Auxiliar (crear/actualizar CONFIG)
            try
            {
                // Crea/abre la BD auxiliar en la ra�z del proyecto (o junto al .exe publicado)
                string auxPath;
                using var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");

                // Prueba de vida
                using (var cmd = new FbCommand("SELECT 1 FROM RDB$DATABASE", auxConn))
                {
                    var ping = cmd.ExecuteScalar();
                    if (Convert.ToInt32(ping) != 1)
                        throw new Exception("Ping fall� en BD auxiliar.");
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
            // 1) Conexi�n/prueba SAE
            try
            {
                // Detecta la BD de SAE (Empresa 01 por defecto)
                string saePath = Sae9Locator.FindSaeDatabase(empresa: 1);

                // Crea conexi�n a SAE (usa tu charset de SAE)
                using var saeConn = SaeDb.CreateConnection(
                    databasePath: saePath,
                    server: "127.0.0.1",
                    port: 3050,
                    user: "SYSDBA",
                    password: "masterkey",
                    charset: "ISO8859_1"
                );

                // Ping + prueba m�nima INVE01 (si existe)
                SaeDb.TestConnection(saeConn);

                MessageBox.Show($"Conexi�n SAE OK.\nFDB: {saePath}", "SAE 9",
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
                string saePath = Sae9Locator.FindSaeDatabase(1);
                using var sae = SaeDb.CreateConnection(saePath, server: "127.0.0.1", port: 3050,
                                                       user: "SYSDBA", password: "masterkey", charset: "ISO8859_1");
                sae.Open();
                using var cmd = new FbCommand(@"
SELECT FIRST 500
       CVE_ART, DESCR, UNI_MED, UNI_ALT, FAC_CONV
FROM INVE01
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
                // fallback: deja la lista vac�a o con dummy
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
                MessageBox.Show("Lectura de peso inv�lida.");
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
                using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
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
            if (keyData == Keys.F5) { IrACobro(); return true; }
            if (keyData == Keys.Delete && dgvPedido.Focused)
            {
                if (_pedidoActual != null && dgvPedido.CurrentRow?.DataBoundItem is PedidoDet d)
                {
                    _pedidoActual.Detalles.Remove(d);
                    RecalcularTotales();
                }
                return true;
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

        private void UpdateScaleTimer()
        {
            _timerBascula.Enabled = chkSimularBascula.Checked || chkInvSimularBascula.Checked;
        }







        private void btnGuardarConfig_Click(object sender, EventArgs e)
        {

            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
            AuxDbInitializer.UpsertConfig(aux, "IMPRESORA_TICKET", cboImpresora.Text);
            AuxDbInitializer.UpsertConfig(aux, "BASCULA_PUERTO", txtPuertoCom.Text);
            AuxDbInitializer.UpsertConfig(aux, "ALMACEN_DEFAULT", cboAlmacen.Text);
            AuxDbInitializer.UpsertConfig(aux, "LISTA_PRECIOS", cboListaPrecios.Text);
            MessageBox.Show("Configuraci�n guardada.");
        }
        //no se usa esto (y no borrar, si no, explota el programa)

        private void txtRutaAux_TextChanged(object sender, EventArgs e)
        {

        }

        private void dgvInvCaptura_CellContentClick(object sender, EventArgs e)
        {

        }


        private void lbInvArticulos_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //hasta aqui lo que no se usa
    }///fin public partial class Form1 : Form
}///fin namespace
//prueba de que se actualize git
//otra prueba   