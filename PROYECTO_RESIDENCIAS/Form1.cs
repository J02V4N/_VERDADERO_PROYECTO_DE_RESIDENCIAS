using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static PROYECTO_RESIDENCIAS.Form1;

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
            public decimal PrecioUnit { get; set; }    // $ por pieza o por kg (según RequierePeso)
            public bool RequierePeso { get; set; }     // true si se vende por gramos (PrecioUnit = $/kg)
            public override string ToString() => $"{Nombre} {(RequierePeso ? $"(${PrecioUnit}/kg)" : $"${PrecioUnit}")}";
        }

        public class PedidoDet
        {
            public string Notas { get; set; }  // <<< NUEVO: notas libres de la partida
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
            // Config inicial del timer de “báscula”
            _timerBascula.Interval = 800; // ms
            _timerBascula.Tick += (s, e) => SimularLecturaBascula();

        }

        private void Form1_Load(object sender, EventArgs e)
        {


            // 1) Carga de catálogos dummy
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
            //var sBas = new ToolStripStatusLabel("Báscula: OFF");
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
            btnInvAplicarCostoTodos.Click += btnInvAplicarCostoTodos_Click;


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

            // Carga inicial del catálogo desde SAE (si quieres al abrir)
            CargarInvArticulosDesdeSAE();



            // ===== Mesas: Context menu y botones =====
            var cmsMesas = new ContextMenuStrip();
            cmsMesas.Items.Add("Atender", null, (s, e) => btnAbrirMesa.PerformClick());
            cmsMesas.Items.Add("Precuenta", null, (s, e) => btnPrecuentaMesa.PerformClick());
            cmsMesas.Items.Add("Transferir", null, (s, e) => btnTransferirMesa.PerformClick());
            cmsMesas.Items.Add("Cambiar mesero", null, (s, e) => btnAsignarMesero.PerformClick());
            cmsMesas.Items.Add("Cerrar", null, (s, e) => btnCerrarMesa.PerformClick());
            cmsMesas.Items.Add("Reabrir", null, (s, e) => btnReabrirMesa.PerformClick());
            dgvMesas.ContextMenuStrip = cmsMesas;

            btnPrecuentaMesa.Click += (s, e) => PrecuentaMesa();
            btnTransferirMesa.Click += (s, e) => TransferirMesa();
            btnCerrarMesa.Click += (s, e) => CerrarMesa();
            btnReabrirMesa.Click += (s, e) => ReabrirMesa();

            // ===== Pedido: botones y menú contextual =====
            btnDuplicarLinea.Click += btnDuplicarLinea_Click;
            btnDividirLinea.Click += btnDividirLinea_Click;
            btnNotasPartida.Click += btnNotasPartida_Click;

            var cmsPedido = new ContextMenuStrip();
            cmsPedido.Items.Add("Duplicar", null, (s, e) => btnDuplicarLinea.PerformClick());
            cmsPedido.Items.Add("Eliminar", null, (s, e) => { if (dgvPedido.Focused) SendKeys.Send("{DELETE}"); });
            cmsPedido.Items.Add("Notas", null, (s, e) => btnNotasPartida.PerformClick());
            dgvPedido.ContextMenuStrip = cmsPedido;

        }


        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        private void UpdateStatus(string who, bool ok)
        {
            if (who == "SAE") tslSae.Text = ok ? "SAE: Conectado" : "SAE: OFF";
            if (who == "AUX") tslAux.Text = ok ? "Aux: Conectada" : "Aux: OFF";
            if (who == "BAS") tslBascula.Text = ok ? "Báscula: ON" : "Báscula: OFF";
        }
        //TOOL STRIP ------------------------------------------------------------------------------------------------------------------------

        // ======== SEED DATA ========

        private void SeedMeseros()
        {
            _meseros = new List<Mesero>
            {
                new Mesero{ Id=1, Nombre="Ana" },
                new Mesero{ Id=2, Nombre="Luis" },
                new Mesero{ Id=3, Nombre="Sofía" }
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
                new Platillo{ Clave="CAM-PS", Nombre="Camarón al peso", PrecioUnit=520m, RequierePeso=true } // $/kg
            };
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
                    Cantidad = p.RequierePeso ? 1 : 1, // si pesa, manejamos PesoGr; Cantidad=1 simbólica
                    PrecioUnit = p.PrecioUnit
                };

                if (p.RequierePeso)
                {
                    // tomar lo que esté en txtPesoGr (simulado o real)
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
            if (_pedidoActual == null || _pedidoActual.Detalles.Count == 0)
                return;

            // Resumen claro
            var meseroNombre = _meseros.FirstOrDefault(x => x.Id == _pedidoActual.MeseroId)?.Nombre ?? "";
            lblResumenCobro.Text = $"Mesa: {_mesaSeleccionada?.Nombre}\n" +
                                   $"Mesero: {meseroNombre}\n" +
                                   $"Partidas: {_pedidoActual.Detalles.Count}\n" +
                                   $"Subtotal: ${_pedidoActual.Subtotal:N2}\n" +
                                   $"IVA: ${_pedidoActual.Impuesto:N2}\n" +
                                   $"TOTAL: ${_pedidoActual.Total:N2}";

            // Facturación (apagada por defecto)
            chkFacturarAhora.Checked = false;
            txtRFC.Enabled = txtRazon.Enabled = cboUsoCFDI.Enabled = false;

            // Cobro: dejar listo para teclear
            if (cboMetodoPago.SelectedIndex < 0 && cboMetodoPago.Items.Count > 0)
                cboMetodoPago.SelectedIndex = 0; // por ej. "Efectivo"

            txtImporteRecibido.Text = string.Empty;
            lblCambio.Text = "Cambio: $0.00";

            // Ir a la pestaña y enfocar el primer control útil
            tabMain.SelectedTab = tabCobro;
            if (cboMetodoPago.Focused == false) cboMetodoPago.Focus();
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

            bool estable;

            // Pedido
            if (chkSimularBascula.Checked && txtPesoGr != null && !txtPesoGr.IsDisposed)
            {
                txtPesoGr.Text = gramos.ToString("0");
                push(gramos);
                estable = EsLecturaEstable();
                ActualizarPesoStatus(estable);

                if (estable && (chkAutoAgregarPesables?.Checked ?? false) &&
                    lbPlatillos.SelectedItem is Platillo p && p.RequierePeso)
                {
                    AgregarPlatilloSeleccionado(); // auto-agrega con el peso estable actual
                    _ultLecturas.Clear();
                }
            }

            // Inventario
            if (chkInvSimularBascula.Checked && txtInvPesoGr != null && !txtInvPesoGr.IsDisposed)
            {
                txtInvPesoGr.Text = gramos.ToString("0");
                lblInvKg.Text = $"{(gramos / 1000m):N3} kg";
                push(gramos);
            }
        }

        private void ActualizarPesoStatus(bool estable)
        {
            if (lblPesoStatus == null) return;
            lblPesoStatus.Text = estable ? "Estable" : "Inestable";
            lblPesoStatus.ForeColor = estable ? Color.ForestGreen : Color.DarkRed;
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
                using var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");

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
                string saePath = Sae9Locator.FindSaeDatabase(empresa: 1);

                // Crea conexión a SAE (usa tu charset de SAE)
                using var saeConn = SaeDb.CreateConnection(
                    databasePath: saePath,
                    server: "127.0.0.1",
                    port: 3050,
                    user: "SYSDBA",
                    password: "masterkey",
                    charset: "ISO8859_1"
                );

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
            if (tabMain.SelectedTab == tabPedido)
            {
                if (keyData == (Keys.Control | Keys.Enter)) { IrACobro(); return true; }
                if (keyData == (Keys.Control | Keys.D) || keyData == Keys.Delete)
                {
                    if (_pedidoActual != null && dgvPedido.Focused && dgvPedido.CurrentRow?.DataBoundItem is PedidoDet d)
                    {
                        _pedidoActual.Detalles.Remove(d);
                        int i = 1; foreach (var x in _pedidoActual.Detalles) x.Partida = i++;
                        RecalcularTotales();
                    }
                    return true;
                }
                if (keyData == Keys.F2)
                {
                    if (decimal.TryParse(txtPesoGr.Text, out var gr) && dgvPedido.CurrentRow?.DataBoundItem is PedidoDet d && d.RequierePeso)
                    { d.PesoGr = gr; dgvPedido.Refresh(); RecalcularTotales(); }
                    return true;
                }
                if (keyData == (Keys.Control | Keys.K)) { txtBuscarPlatillo.Focus(); txtBuscarPlatillo.SelectAll(); return true; }
            }

            if (keyData == Keys.Escape) { tabMain.SelectedTab = tabMesas; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
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
            decimal.TryParse(txtImporteRecibido.Text, out var rec);
            var cambio = Math.Max(0, rec - (_pedidoActual?.Total ?? 0m));
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
                if (rec < (_pedidoActual?.Total ?? 0m)) { MessageBox.Show("Importe insuficiente."); return false; }
                var cambio = rec - (_pedidoActual?.Total ?? 0m);
                lblCambio.Text = $"Cambio: ${cambio:N2}";
            }
            return true;
        }


        private void CargarConfigUI()
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
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
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
            AuxDbInitializer.UpsertConfig(aux, "BASCULA_PUERTO", txtPuertoCom.Text);
            AuxDbInitializer.UpsertConfig(aux, "IMPRESORA_TICKET", cboImpresora.Text);
            AuxDbInitializer.UpsertConfig(aux, "ALMACEN_DEFAULT", cboAlmacen.Text);
            AuxDbInitializer.UpsertConfig(aux, "LISTA_PRECIOS", cboListaPrecios.Text);
        }


        private void btnConfirmarCobro_Click(object sender, EventArgs e)
        {
            if (!ValidarCobro()) return;
            ConfirmarCobro();
        }



        private void btnGuardarConfig_Click(object sender, EventArgs e)
        {

            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8");
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


        // ===== Mesas =====
        private void PrecuentaMesa()
        {
            if (_mesaSeleccionada == null) return;
            MessageBox.Show($"Precuenta de {_mesaSeleccionada.Nombre}\n" +
                            $"{_pedidoActual?.Detalles.Count ?? 0} partidas, Total: ${_pedidoActual?.Total ?? 0m:N2}",
                            "Precuenta", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TransferirMesa()
        {
            if (_mesaSeleccionada == null) return;
            // Maquetado: solo diálogo
            MessageBox.Show("Transferir mesa (maquetado). Aquí abrirías un selector de mesa destino.", "Transferir");
        }

        private void CerrarMesa()
        {
            if (_mesaSeleccionada == null) return;
            if (!CambiarEstadoMesa(_mesaSeleccionada, MesaEstado.CERRADA)) return;
            _pedidosAbiertos.Remove(_mesaSeleccionada.Id);
            _pedidoActual = null; dgvPedido.DataSource = null; ActualizarUI();
        }

        private void ReabrirMesa()
        {
            if (_mesaSeleccionada == null) return;
            CambiarEstadoMesa(_mesaSeleccionada, MesaEstado.LIBRE);
            ActualizarUI();
        }

        // ===== Pedido =====
        private void btnDuplicarLinea_Click(object sender, EventArgs e)
        {
            if (_pedidoActual == null) return;
            if (dgvPedido.CurrentRow?.DataBoundItem is PedidoDet d)
            {
                var copy = new PedidoDet
                {
                    Partida = _pedidoActual.Detalles.Count + 1,
                    Clave = d.Clave,
                    Nombre = d.Nombre,
                    Cantidad = d.Cantidad,
                    PesoGr = d.PesoGr,
                    PrecioUnit = d.PrecioUnit,
                    RequierePeso = d.RequierePeso,
                    Notas = d.Notas
                };
                _pedidoActual.Detalles.Add(copy);
                ReindexDetalles(); RecalcularTotales();
            }
        }

        private void btnDividirLinea_Click(object sender, EventArgs e)
        {
            if (_pedidoActual == null) return;
            if (dgvPedido.CurrentRow?.DataBoundItem is PedidoDet d)
            {
                if (d.RequierePeso && (d.PesoGr ?? 0m) > 1)
                {
                    var mitad = Math.Round((d.PesoGr ?? 0m) / 2m, 0);
                    d.PesoGr = mitad;
                    var copy = new PedidoDet
                    {
                        Partida = _pedidoActual.Detalles.Count + 1,
                        Clave = d.Clave,
                        Nombre = d.Nombre,
                        RequierePeso = true,
                        PesoGr = mitad,
                        Cantidad = 1,
                        PrecioUnit = d.PrecioUnit,
                        Notas = d.Notas
                    };
                    _pedidoActual.Detalles.Add(copy);
                }
                else if (!d.RequierePeso && d.Cantidad > 1m)
                {
                    var mitad = Math.Round(d.Cantidad / 2m, 2);
                    d.Cantidad = mitad;
                    var copy = new PedidoDet
                    {
                        Partida = _pedidoActual.Detalles.Count + 1,
                        Clave = d.Clave,
                        Nombre = d.Nombre,
                        RequierePeso = false,
                        Cantidad = mitad,
                        PrecioUnit = d.PrecioUnit,
                        Notas = d.Notas
                    };
                    _pedidoActual.Detalles.Add(copy);
                }
                ReindexDetalles(); RecalcularTotales();
            }
        }

        private void btnNotasPartida_Click(object sender, EventArgs e)
        {
            if (_pedidoActual == null) return;
            if (dgvPedido.CurrentRow?.DataBoundItem is PedidoDet d)
            {
                string nota = PromptNotas(d.Notas);
                if (nota != null) { d.Notas = nota; dgvPedido.Refresh(); }
            }
        }

        private void ReindexDetalles()
        {
            int i = 1; foreach (var x in _pedidoActual.Detalles) x.Partida = i++;
            dgvPedido.Refresh();
        }

        // InputBox simple para notas
        private string PromptNotas(string actual)
        {
            using var f = new Form { Width = 400, Height = 180, Text = "Notas", StartPosition = FormStartPosition.CenterParent };
            var txt = new TextBox { Left = 10, Top = 10, Width = 360, Text = actual ?? "" };
            var ok = new Button { Text = "OK", Left = 210, Top = 60, DialogResult = DialogResult.OK };
            var cl = new Button { Text = "Cancelar", Left = 290, Top = 60, DialogResult = DialogResult.Cancel };
            f.Controls.AddRange(new Control[] { txt, ok, cl }); f.AcceptButton = ok; f.CancelButton = cl;
            return f.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }



        //no se usa esto (y no borrar, si no, explota el programa)///////////////////////////////////////////////////////////////////////////////////

        private void txtRutaAux_TextChanged(object sender, EventArgs e)
        {

        }

        private void dgvInvCaptura_CellContentClick(object sender, EventArgs e)
        {

        }

        private void btnAbrirMesa_Click(object sender, EventArgs e)
        {

        }

        private void btnReimprimir_Click(object sender, EventArgs e)
        {

        }

        //hasta aqui lo que no se usa////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }///fin public partial class Form1 : Form
}///fin namespace
