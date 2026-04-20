using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using FirebirdSql.Data.FirebirdClient;


namespace GastroSAE
{
    public partial class FormRecetaEditor : Form
    {
        private DataTable _dtPlatillos;   // de SAE (clave/desc/unidad/precio)
        private DataTable _dtIngredientes;// de SAE (usaremos INVE como catálogo de “ingredientes”)
        private string _cveArtActual;     // CVE_ART del platillo elegido

        private const decimal TASA_IVA = 0.16m;

        private CheckBox chkPrecioManual = null!;
        private NumericUpDown nudPrecioPublico = null!;
        private Label lblPrecioPublico = null!;
        private Label lblIvaTotal = null!;

        public bool DataChanged { get; private set; }
        public string ChangedCveArt { get; private set; } = string.Empty;
        public decimal ChangedPrecioPublico { get; private set; }

        private bool _suppressDirtyTracking;
        private bool _suppressIngredientWarnings;
        private string _snapshotFirma = string.Empty;

        public FormRecetaEditor()
        {
            InitializeComponent();
            AppIcon.Apply(this);

            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(900, 600);

            UiStyle.Apply(this);
            UiFields.Apply(this);

            chkPrecioManual = new CheckBox
            {
                Name = "chkPrecioManual",
                Text = "Precio manual (precio final)",
                AutoSize = true,
                Margin = new Padding(0, 4, 8, 0)
            };
            nudPrecioPublico = new NumericUpDown
            {
                Name = "nudPrecioPublico",
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Maximum = 1000000m,
                Minimum = 0m,
                Width = 140,
                TextAlign = HorizontalAlignment.Right,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 0)
            };
            lblPrecioPublico = new Label
            {
                Name = "lblPrecioPublico",
                Text = "Precio público total:",
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            lblIvaTotal = new Label
            {
                Name = "lblIvaTotal",
                Text = "IVA total receta: $0.00",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            chkPrecioManual.CheckedChanged += (_, __) =>
            {
                nudPrecioPublico.Enabled = chkPrecioManual.Checked;
                if (!chkPrecioManual.Checked)
                    SyncPrecioConCosto();
                else
                    ActualizarIvaVisual();
                MarcarCambiosSiAplica();
            };

            nudPrecioPublico.ValueChanged += (_, __) =>
            {
                if (chkPrecioManual.Checked)
                    ActualizarIvaVisual();
                MarcarCambiosSiAplica();
            };

            nudPrecioPublico.TextChanged += (_, __) =>
            {
                if (chkPrecioManual.Checked)
                    ActualizarIvaVisual();
                MarcarCambiosSiAplica();
            };

            // Layout responsive (sin encimados) para que la ventana no “desperdicie” espacio
            // y los botones no se monten unos sobre otros.
            RefactorLayout();

            UiHints.Attach(this, new System.Collections.Generic.Dictionary<string, string>
            {
                ["cboPlatillo"] = "Ctrl+F — Buscar",
                ["btnNuevoPlatillo"] = "Ctrl+N",
                ["btnAgregarFila"] = "Ins",
                ["btnEliminarProducto"] = "Del",
                ["btnEliminarTodo"] = "Ctrl+Del",
                ["btnGuardar"] = "Ctrl+S",
                ["chkPrecioManual"] = "Precio de venta libre",
            });

            Shown += (_, __) =>
            {
                try
                {
                    Init();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error inicializando el editor de recetas:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private void RefactorLayout()
        {
            try
            {
                SuspendLayout();

                // Guardar referencias (son los mismos controles del Designer)
                var header = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 3,
                    RowCount = 1,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 0, 0, 10)
                };
                header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                lblPlatillo.Dock = DockStyle.Fill;
                lblPlatillo.Margin = new Padding(0, 6, 10, 0);

                cboPlatillo.Dock = DockStyle.Fill;
                cboPlatillo.Margin = new Padding(0, 0, 10, 0);

                var flHeaderBtns = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0)
                };
                btnNuevoPlatillo.MinimumSize = new Size(140, 44);
                btnEliminarProducto.MinimumSize = new Size(170, 44);
                flHeaderBtns.Controls.Add(btnNuevoPlatillo);
                flHeaderBtns.Controls.Add(btnEliminarProducto);

                header.Controls.Add(lblPlatillo, 0, 0);
                header.Controls.Add(cboPlatillo, 1, 0);
                header.Controls.Add(flHeaderBtns, 2, 0);

                dgvIngredientes.Dock = DockStyle.Fill;
                dgvIngredientes.Margin = new Padding(0);

                // Footer: acciones + total
                var footer = new TableLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    ColumnCount = 2,
                    RowCount = 1,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 10, 0, 0)
                };
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                var flActions = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    WrapContents = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0)
                };

                btnAgregarFila.MinimumSize = new Size(130, 44);
                btnEliminarTodo.MinimumSize = new Size(150, 44);
                btnGuardar.MinimumSize = new Size(130, 44);

                flActions.Controls.Add(btnAgregarFila);
                flActions.Controls.Add(btnEliminarTodo);
                flActions.Controls.Add(btnGuardar);

                lblTotal.AutoSize = true;
                lblTotal.Anchor = AnchorStyles.Right;
                lblTotal.Margin = new Padding(20, 0, 0, 0);

                var pnlPricing = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    ColumnCount = 1,
                    RowCount = 3,
                    BackColor = Color.Transparent,
                    Margin = new Padding(20, 0, 0, 0)
                };
                pnlPricing.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                pnlPricing.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                pnlPricing.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                pnlPricing.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var flPrecio = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 4, 0, 0)
                };
                flPrecio.Controls.Add(chkPrecioManual);
                flPrecio.Controls.Add(lblPrecioPublico);
                flPrecio.Controls.Add(nudPrecioPublico);

                pnlPricing.Controls.Add(lblTotal, 0, 0);
                pnlPricing.Controls.Add(flPrecio, 0, 1);
                pnlPricing.Controls.Add(lblIvaTotal, 0, 2);

                footer.Controls.Add(flActions, 0, 0);
                footer.Controls.Add(pnlPricing, 1, 0);

                // Root
                Controls.Clear();
                var root = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(12),
                    BackColor = Color.Transparent
                };
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                root.Controls.Add(header, 0, 0);
                root.Controls.Add(dgvIngredientes, 0, 1);
                root.Controls.Add(footer, 0, 2);

                Controls.Add(root);
                ResumeLayout(true);
            }
            catch
            {
                // nunca romper la ventana por layout
            }
        }

        private void SetDirtyTracking(bool enabled)
        {
            _suppressDirtyTracking = !enabled;
        }

        private void MarcarCambiosSiAplica()
        {
            if (_suppressDirtyTracking)
                return;

            var actual = ObtenerFirmaActual();
            var dirty = !string.IsNullOrWhiteSpace(_cveArtActual) && !string.Equals(actual, _snapshotFirma, StringComparison.Ordinal);
            btnGuardar.Enabled = dirty;
        }

        private void TomarSnapshotActual()
        {
            _snapshotFirma = ObtenerFirmaActual();
            btnGuardar.Enabled = false;
        }

        private string ObtenerFirmaActual()
        {
            var partes = new System.Collections.Generic.List<string>();
            partes.Add((_cveArtActual ?? string.Empty).Trim());
            partes.Add(chkPrecioManual.Checked ? "1" : "0");
            partes.Add(GetPrecioManualTotalActual().ToString("0.####", CultureInfo.InvariantCulture));

            foreach (DataGridViewRow r in dgvIngredientes.Rows)
            {
                if (r.IsNewRow) continue;
                var ing = (r.Cells["colIngrediente"].Value?.ToString() ?? string.Empty).Trim();
                var um = (r.Cells["colUnidad"].Value?.ToString() ?? string.Empty).Trim();
                var cant = ToDec(r.Cells["colCantidad"].Value).ToString("0.####", CultureInfo.InvariantCulture);
                var costo = ToDec(r.Cells["colCostoUnit"].Value).ToString("0.####", CultureInfo.InvariantCulture);
                partes.Add($"{ing}|{um}|{cant}|{costo}");
            }

            return string.Join(";", partes);
        }

        private void LimpiarRecetaActual()
        {
            SetDirtyTracking(false);
            _cveArtActual = string.Empty;
            dgvIngredientes.Rows.Clear();
            lblTotal.Text = "Costo total receta: 0.00";
            chkPrecioManual.Checked = false;
            nudPrecioPublico.Enabled = false;
            nudPrecioPublico.Value = 0m;
            ActualizarIvaVisual(0m);
            TomarSnapshotActual();
            SetDirtyTracking(true);
        }

        private void Init()
        {
            // 1) Carga catálogos desde SAE
            CargarPlatillosDesdeSae();
            CargarIngredientesDesdeSae();

            // 2) Prepara grid (ver sección columnas abajo)
            PrepararGrid();

            // 3) Eventos
            cboPlatillo.SelectedIndexChanged += (_, __) => CargarRecetaDelPlatillo();
            btnAgregarFila.Click += btnAgregarFila_Click;
            btnGuardar.Click += btnGuardar_Click;
            btnEliminarTodo.Click += btnEliminarTodo_Click;

            dgvIngredientes.CellValueChanged += dgvIngredientes_CellValueChanged;
            dgvIngredientes.CellContentClick += dgvIngredientes_CellContentClick;
            dgvIngredientes.RowsAdded += (_, __) => { RecalcTotales(); MarcarCambiosSiAplica(); };
            dgvIngredientes.RowsRemoved += (_, __) => { RecalcTotales(); MarcarCambiosSiAplica(); };

            btnGuardar.Enabled = false;
            if (cboPlatillo.Items.Count > 0)
                cboPlatillo.SelectedIndex = 0;
        }


        // ========= AUX SCHEMA =========





        // ========= SAE CATALOGS =========

        private void CargarPlatillosDesdeSae()
        {
            // Reusa el mismo origen que usas en Form1: SaeDb.ListarPlatillos(lista, almacen)
            int lista = 1, alm = 1;
            // Si tienes CONFIG en AUX, puedes leerlos aquí igual que en Form1 (opcional).

            var rows = SaeDb.ListarPlatillos(listaPrecio: lista, almacen: alm);
            _dtPlatillos = new DataTable();
            _dtPlatillos.Columns.Add("CLAVE", typeof(string));
            _dtPlatillos.Columns.Add("NOMBRE", typeof(string));
            _dtPlatillos.Rows.Add(string.Empty, "Seleccionar platillo");
            foreach (var r in rows)
                _dtPlatillos.Rows.Add(r.Clave, r.Descripcion);

            cboPlatillo.DisplayMember = "NOMBRE";
            cboPlatillo.ValueMember = "CLAVE";
            cboPlatillo.DataSource = _dtPlatillos;
        }

        private void CargarIngredientesDesdeSae()
        {
            // Usaremos INVE completo como catálogo de “ingredientes”.
            using var con = SaeDb.GetOpenConnection();
            string tINVE = SaeDb.GetTableName(con, "INVE");

            using var cmd = new FbCommand($@"
SELECT CVE_ART, DESCR, UNI_MED, UNI_ALT, FAC_CONV, COSTO_PROM, COALESCE(EXIST, 0) AS EXIST
FROM {tINVE}
WHERE (STATUS IS NULL OR STATUS <> 'B')
  AND COALESCE(LIN_PROD, '') = 'Insum'
  AND COALESCE(TIPO_ELE, 'P') = 'P'
ORDER BY DESCR", con);

            using var rd = cmd.ExecuteReader();
            _dtIngredientes = new DataTable();
            _dtIngredientes.Columns.Add("CLAVE", typeof(string));
            _dtIngredientes.Columns.Add("NOMBRE", typeof(string));
            _dtIngredientes.Columns.Add("UNIDAD_BASE", typeof(string));
            _dtIngredientes.Columns.Add("UNIDAD_CAPTURA", typeof(string));
            _dtIngredientes.Columns.Add("FAC_CONV", typeof(decimal));
            _dtIngredientes.Columns.Add("COSTO", typeof(decimal));
            _dtIngredientes.Columns.Add("EXISTENCIA", typeof(decimal));
            while (rd.Read())
            {
                var baseUnit = rd.IsDBNull(2) ? string.Empty : rd.GetString(2).Trim();
                var altUnit = rd.IsDBNull(3) ? string.Empty : rd.GetString(3).Trim();
                var facConv = rd.IsDBNull(4) ? 1m : Convert.ToDecimal(rd.GetValue(4));
                var profile = SaeCatalogAdmin.ResolveUnitProfile(baseUnit, altUnit, facConv);

                _dtIngredientes.Rows.Add(
                    rd.IsDBNull(0) ? "" : rd.GetString(0).Trim(),
                    rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                    profile.UniAlt,
                    profile.UniMed,
                    profile.FacConv,
                    rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5)),
                    rd.IsDBNull(6) ? 0m : Convert.ToDecimal(rd.GetValue(6)));
            }
        }

        // ========= GRID & UI =========

        private void PrepararGrid()
        {
            dgvIngredientes.AutoGenerateColumns = false;
            dgvIngredientes.Columns.Clear();

            var colIng = new DataGridViewComboBoxColumn
            {
                Name = "colIngrediente",
                HeaderText = "Ingrediente",
                Width = 260,
                DataSource = _dtIngredientes,
                DisplayMember = "NOMBRE",
                ValueMember = "CLAVE",
                FlatStyle = FlatStyle.Flat
            };
            dgvIngredientes.Columns.Add(colIng);

            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnidad", HeaderText = "UM", ReadOnly = true, Width = 70 });

            // SAE -> KITS.CANTIDAD
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCantidad", HeaderText = "Cantidad", Width = 90 });

            // Costo referencial (INVE.COSTO_PROM)
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCostoUnit", HeaderText = "Costo Unit", ReadOnly = true, Width = 90 });
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCostoTotal", HeaderText = "Costo Total", ReadOnly = true, Width = 110 });

            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotas", HeaderText = "Notas (solo UI)", Width = 200 });

            dgvIngredientes.Columns.Add(new DataGridViewButtonColumn { Name = "colEliminar", HeaderText = "", Text = "X", UseColumnTextForButtonValue = true, Width = 40 });
        }
        private void CargarRecetaDelPlatillo()
        {
            if (cboPlatillo.SelectedValue == null)
            {
                LimpiarRecetaActual();
                return;
            }

            var seleccion = cboPlatillo.SelectedValue.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(seleccion))
            {
                LimpiarRecetaActual();
                return;
            }

            SetDirtyTracking(false);
            _cveArtActual = seleccion;

            dgvIngredientes.Rows.Clear();
            _suppressIngredientWarnings = true;

            try
            {
                using var con = SaeDb.GetOpenConnection();
                string tKITS = SaeDb.GetTableName(con, "KITS"); // KITS01..KITS99
                string tINVE = SaeDb.GetTableName(con, "INVE");

                var sql = $@"
SELECT
  K.CVE_PROD,                          -- 0 ingrediente
  I.DESCR,                             -- 1 nombre
  I.UNI_MED,                           -- 2 unidad base
  I.UNI_ALT,                           -- 3 unidad captura
  COALESCE(I.FAC_CONV, 1),             -- 4 factor de conversión
  COALESCE(K.CANTIDAD, 0),             -- 5 cantidad capturada
  COALESCE(I.COSTO_PROM, 0)            -- 6 costo unitario base
FROM {tKITS} K
LEFT JOIN {tINVE} I ON I.CVE_ART = K.CVE_PROD
WHERE K.CVE_ART = @C
ORDER BY K.CVE_PROD";

                using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(sql, con);
                cmd.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = _cveArtActual;

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    int idx = dgvIngredientes.Rows.Add();
                    var row = dgvIngredientes.Rows[idx];

                    string cveIng = rd.IsDBNull(0) ? "" : rd.GetString(0).Trim();
                    row.Cells["colIngrediente"].Value = cveIng;
                    var baseUnit = rd.IsDBNull(2) ? string.Empty : rd.GetString(2).Trim();
                    var altUnit = rd.IsDBNull(3) ? string.Empty : rd.GetString(3).Trim();
                    var facConv = rd.IsDBNull(4) ? 1m : Convert.ToDecimal(rd.GetValue(4));
                    var profile = SaeCatalogAdmin.ResolveUnitProfile(baseUnit, altUnit, facConv);
                    var cantidadKit = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5));
                    row.Cells["colUnidad"].Value = profile.UniAlt;
                    row.Cells["colCantidad"].Value = SaeCatalogAdmin.NormalizeKitQtyForRuntime(profile.UniAlt, profile.UniMed, profile.FacConv, cantidadKit);
                    row.Cells["colCostoUnit"].Value = rd.IsDBNull(6) ? 0m : Convert.ToDecimal(rd.GetValue(6));

                    RecalcFila(row);
                }

                RecalcTotales();
                CargarPrecioDesdeSae();
                TomarSnapshotActual();
                SetDirtyTracking(true);
            }
            finally
            {
                _suppressIngredientWarnings = false;
            }
        }



        private int GetOrCreateRecetaId(string cveArt)
        {
            string path;
            using var con = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");

            // Busca
            using (var cmd = new FbCommand(@"SELECT ID_RECETA FROM RECETA WHERE CVE_ART=@C", con))
            {
                cmd.Parameters.Add("@C", FbDbType.VarChar, 30).Value = cveArt;
                var o = cmd.ExecuteScalar();
                if (o != null) return Convert.ToInt32(o);
            }

            // Crea
            using (var cmdIns = new FbCommand(@"INSERT INTO RECETA (CVE_ART) VALUES (@C) RETURNING ID_RECETA", con))
            {
                cmdIns.Parameters.Add("@C", FbDbType.VarChar, 30).Value = cveArt;
                return Convert.ToInt32(cmdIns.ExecuteScalar());
            }
        }

        // ========= Botones =========
        private void btnAgregarFila_Click(object sender, EventArgs e)
        {
            int idx = dgvIngredientes.Rows.Add();
            var row = dgvIngredientes.Rows[idx];
            row.Cells["colCantidad"].Value = 0m;
            row.Cells["colCostoUnit"].Value = 0m;
            row.Cells["colCostoTotal"].Value = 0m;
        }

        private void btnEliminarTodo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_cveArtActual)) return;
            if (MessageBox.Show("¿Eliminar TODA la receta (KITS) de este platillo en SAE?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No) return;

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tKITS = SaeDb.GetTableName(con, "KITS", tx);  // con tx
                                                                 // ... comandos con (con, tx)


            // (Opcional) consulta previa de conteo: DEBE llevar tx
            using (var cmdCount = new FirebirdSql.Data.FirebirdClient.FbCommand(
                $@"SELECT COUNT(*) FROM {tKITS} WHERE CVE_ART=@C", con, tx))
            {
                cmdCount.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30)
                       .Value = _cveArtActual;

                var count = Convert.ToInt32(cmdCount.ExecuteScalar());
                // Si quieres, puedes preguntar con base en count...
            }

            // DELETE con tx
            using (var cmdDel = new FirebirdSql.Data.FirebirdClient.FbCommand(
                $@"DELETE FROM {tKITS} WHERE CVE_ART=@C", con, tx))
            {
                cmdDel.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30)
                      .Value = _cveArtActual;

                cmdDel.ExecuteNonQuery();
            }

            tx.Commit();

            dgvIngredientes.Rows.Clear();
            RecalcTotales();
        }



        private void btnEliminarProducto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_cveArtActual)) return;

            // 1) Confirmar
            var resp = MessageBox.Show(
                "Esto dará de baja el producto en SAE (STATUS='B') y eliminará su receta (KITS). ¿Continuar?",
                "Confirmar eliminación de producto",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (resp != DialogResult.Yes) return;

            using var con = SaeDb.GetOpenConnection();

            // ⚠️ IMPORTANTE: resuelve nombres ANTES de abrir tx, o pásale tx si modificaste SaeDb para aceptarla
            string tINVE = SaeDb.GetTableName(con, "INVE");
            string tKITS = SaeDb.GetTableName(con, "KITS");

            using var tx = con.BeginTransaction();
            try
            {
                // (Opcional) Verifica si el producto está usado como componente en otras recetas
                using (var cmdUsed = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"SELECT COUNT(*) FROM {tKITS} WHERE CVE_PROD=@C", con, tx))
                {
                    cmdUsed.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = _cveArtActual;
                    var usedCount = Convert.ToInt32(cmdUsed.ExecuteScalar());
                    if (usedCount > 0)
                    {
                        var r2 = MessageBox.Show(
                            $"Este producto aparece como ingrediente en {usedCount} receta(s). " +
                            $"Se eliminará la receta del propio producto, pero no se tocarán esas otras recetas.\n\n¿Deseas continuar?",
                            "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                        if (r2 != DialogResult.Yes)
                        {
                            tx.Rollback();
                            return;
                        }
                    }
                }

                // 2) Elimina la receta de ESTE producto (como ya haces en 'Eliminar todo')
                using (var cmdDelKit = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"DELETE FROM {tKITS} WHERE CVE_ART=@C", con, tx))
                {
                    cmdDelKit.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = _cveArtActual;
                    cmdDelKit.ExecuteNonQuery();
                }

                // 3) BAJA LÓGICA: marcar STATUS='B' en INVE en lugar de borrar
                using (var cmdUpdInv = new FbCommand(
                    $@"UPDATE {tINVE}
       SET STATUS = 'B'
       WHERE CVE_ART = @C", con, tx))
                {
                    cmdUpdInv.Parameters.Add("@C", FbDbType.VarChar, 30).Value = _cveArtActual;
                    var rows = cmdUpdInv.ExecuteNonQuery();
                    if (rows == 0)
                        throw new Exception("No se encontró el producto en INVE para darlo de baja.");
                }



                tx.Commit();

                // Limpia UI
                dgvIngredientes.Rows.Clear();
                RecalcTotales();

                MessageBox.Show("Producto dado de baja y receta eliminada en SAE.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                MessageBox.Show("Error eliminando el producto:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnNuevoPlatillo_Click(object sender, EventArgs e)
        {
            using var f = new FormNuevoProducto();
            if (f.ShowDialog(this) != DialogResult.OK) return; // usuario canceló

            // Toma datos ya validados/normalizados desde el form
            var cveArt = f.CveArt.Trim();           // ya empieza con "Prep" y <=16
            var descr = f.Descripcion.Trim();      // <=40
            var um = (f.Unidad ?? "pz").Trim();

            try
            {
                using var con = SaeDb.GetOpenConnection();
                // Resuelve tablas ANTES de abrir tx (o pásale tx si tu SaeDb ya acepta)
                string tINVE = SaeDb.GetTableName(con, "INVE");

                using var tx = con.BeginTransaction();

                // 1) Verifica duplicado
                using (var cmdChk = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"SELECT COUNT(*) FROM {tINVE} WHERE CVE_ART=@C", con, tx))
                {
                    cmdChk.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = cveArt;
                    var exists = Convert.ToInt32(cmdChk.ExecuteScalar()) > 0;
                    if (exists) throw new Exception("Ya existe un producto con esa clave en SAE.");
                }

                // 2) Asegura líneas base y crea el platillo como KIT en la línea Prep
                SaeCatalogAdmin.EnsureBaseLines(con, tx);

                var sql = $@"
INSERT INTO {tINVE} (CVE_ART, DESCR, UNI_MED, UNI_ALT, FAC_CONV, UNI_EMP, TIPO_ELE, STATUS, LIN_PROD, CON_SERIE, TIP_COSTEO, NUM_MON, COSTO_PROM, ULT_COSTO, CTRL_ALM, TIEM_SURT, STOCK_MIN, STOCK_MAX, COMP_X_REC, PEND_SURT, EXIST, APART, CON_LOTE, CON_PEDIMENTO, PESO, VOLUMEN, CVE_OBS, CVE_ESQIMPU, BLK_CST_EXT, MAN_IEPS, APL_MAN_IMP, CUOTA_IEPS, APL_MAN_IEPS)
VALUES (@CVE_ART, @DESCR, @UNI_MED, @UNI_ALT, @FAC_CONV, @UNI_EMP, @TIPO_ELE, @STATUS, @LIN_PROD, @CON_SERIE, 'P', 1, 0, 0, @CTRL_ALM, 0, 0, 0, 0, 0, 0, 0, 'N', 'N', 0, 0, 0, 1, 'N', 'N', 1, 0, 'C')";
                using (var cmdIns = new FirebirdSql.Data.FirebirdClient.FbCommand(sql, con, tx))
                {
                    cmdIns.Parameters.Add("@CVE_ART", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 16).Value = cveArt;
                    cmdIns.Parameters.Add("@DESCR", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 40).Value = descr;
                    cmdIns.Parameters.Add("@UNI_MED", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 10).Value = um;
                    cmdIns.Parameters.Add("@UNI_ALT", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 10).Value = "pz";
                    cmdIns.Parameters.Add("@FAC_CONV", FirebirdSql.Data.FirebirdClient.FbDbType.Double).Value = 1d;
                    cmdIns.Parameters.Add("@UNI_EMP", FirebirdSql.Data.FirebirdClient.FbDbType.Double).Value = 1d;
                    cmdIns.Parameters.Add("@CTRL_ALM", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 1).Value = string.Empty;
                    cmdIns.Parameters.Add("@TIPO_ELE", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 1).Value = "K";
                    cmdIns.Parameters.Add("@STATUS", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 1).Value = "A";
                    cmdIns.Parameters.Add("@LIN_PROD", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 5).Value = SaeCatalogAdmin.LinePrep;
                    cmdIns.Parameters.Add("@CON_SERIE", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 1).Value = "N";
                    cmdIns.ExecuteNonQuery();
                }

                SaeCatalogAdmin.EnsureArticuloEnAlmacen(con, tx, cveArt, 1);
                SaeCatalogAdmin.UpsertPrecioPublico(con, tx, cveArt, 0m, 1);
                SaeCatalogAdmin.ApplyNativeInveDefaults(con, tx, cveArt, esKit: true);

                tx.Commit();

                // 3) Refresca el combo y selecciona el nuevo
                CargarPlatillosDesdeSae(); // tu método que llena cboPlatillo desde INVE/SAE
                foreach (DataRowView item in cboPlatillo.Items)
                {
                    if (string.Equals(item["CLAVE"]?.ToString()?.Trim(), cveArt, StringComparison.OrdinalIgnoreCase))
                    {
                        cboPlatillo.SelectedItem = item;
                        break;
                    }
                }

                MessageBox.Show(
                    "Producto creado en SAE como KIT.\nAhora agrega sus componentes y guarda la receta.",
                    "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo crear el producto:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_cveArtActual)) return;

            // Validaciones
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow r in dgvIngredientes.Rows)
            {
                if (r.IsNewRow) continue;

                var ingrediente = (r.Cells["colIngrediente"].Value?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(ingrediente))
                {
                    MessageBox.Show("Hay filas sin ingrediente.");
                    return;
                }

                if (!decimal.TryParse(r.Cells["colCantidad"].Value?.ToString(), out _))
                {
                    MessageBox.Show("Hay filas sin cantidad válida.");
                    return;
                }

                if (!usados.Add(ingrediente))
                {
                    MessageBox.Show(
                        $"El ingrediente '{ingrediente}' está repetido en la receta.\n\n" +
                        "En SAE, cada ingrediente solo puede aparecer una vez por receta. " +
                        "Une la cantidad en una sola línea y vuelve a guardar.",
                        "Ingrediente duplicado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            try
            {
                string tKITS = SaeDb.GetTableName(con, "KITS", tx);
                string tINVE = SaeDb.GetTableName(con, "INVE", tx);

                using (var cmdDel = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"DELETE FROM {tKITS} WHERE CVE_ART=@C", con, tx))
                {
                    cmdDel.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30)
                          .Value = _cveArtActual;
                    cmdDel.ExecuteNonQuery();
                }

                using var cmdIns = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"INSERT INTO {tKITS} (CVE_ART, CVE_PROD, PORCEN, CANTIDAD)
       VALUES (@CVE_ART, @CVE_PROD, @PORCEN, @CANT)", con, tx);

                cmdIns.Parameters.Add("@CVE_ART", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30);
                cmdIns.Parameters.Add("@CVE_PROD", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30);
                cmdIns.Parameters.Add("@PORCEN", FirebirdSql.Data.FirebirdClient.FbDbType.Decimal);
                cmdIns.Parameters.Add("@CANT", FirebirdSql.Data.FirebirdClient.FbDbType.Decimal);

                decimal costoTotalReceta = 0m;
                decimal precioPublicoRecetaBase;
                decimal precioPublicoRecetaTotal;
                foreach (DataGridViewRow r in dgvIngredientes.Rows)
                {
                    if (r.IsNewRow) continue;

                    cmdIns.Parameters["@CVE_ART"].Value = _cveArtActual;
                    cmdIns.Parameters["@CVE_PROD"].Value = (r.Cells["colIngrediente"].Value?.ToString() ?? "").Trim();
                    cmdIns.Parameters["@PORCEN"].Value = 0m;
                    cmdIns.Parameters["@CANT"].Value = ToDec(r.Cells["colCantidad"].Value);
                    cmdIns.ExecuteNonQuery();

                    costoTotalReceta += ToDec(r.Cells["colCostoTotal"].Value);
                }

                using (var cmdUpdCost = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"UPDATE {tINVE}
       SET COSTO_PROM = 0,
           ULT_COSTO = 0,
           EXIST = 0,
           UNI_MED = COALESCE(NULLIF(TRIM(UNI_MED), ''), 'pz'),
           UNI_ALT = COALESCE(NULLIF(TRIM(UNI_ALT), ''), 'pz'),
           FAC_CONV = COALESCE(NULLIF(FAC_CONV, 0), 1),
           UNI_EMP = COALESCE(NULLIF(UNI_EMP, 0), 1),
           STATUS = 'A',
           TIPO_ELE = 'K',
           LIN_PROD = @LIN,
           CON_SERIE = 'N',
           TIP_COSTEO = 'P',
           NUM_MON = 1,
           CTRL_ALM = '',
           TIEM_SURT = 0,
           STOCK_MIN = 0,
           STOCK_MAX = 0,
           FCH_ULTCOM = NULL,
           FCH_ULTVTA = @FCH_ULTVTA,
           PEND_SURT = 0,
           CVE_OBS = 0,
           APART = 0,
           CON_LOTE = 'N',
           CON_PEDIMENTO = 'N',
           PESO = 0,
           VOLUMEN = 0,
           CVE_ESQIMPU = 1,
           BLK_CST_EXT = 'N',
           MAN_IEPS = 'N',
           APL_MAN_IMP = 1,
           CUOTA_IEPS = 0,
           APL_MAN_IEPS = 'C'
       WHERE CVE_ART = @C", con, tx))
                {
                    cmdUpdCost.Parameters.Add("@COSTO", FirebirdSql.Data.FirebirdClient.FbDbType.Double).Value = Convert.ToDouble(costoTotalReceta);
                    cmdUpdCost.Parameters.Add("@LIN", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 5).Value = SaeCatalogAdmin.LinePrep;
                    cmdUpdCost.Parameters.Add("@FCH_ULTVTA", FirebirdSql.Data.FirebirdClient.FbDbType.TimeStamp).Value = DateTime.Today;
                    cmdUpdCost.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = _cveArtActual;
                    cmdUpdCost.ExecuteNonQuery();
                }

                SaeCatalogAdmin.EnsureBaseLines(con, tx);
                SaeCatalogAdmin.EnsureArticuloEnAlmacen(con, tx, _cveArtActual, 1);
                precioPublicoRecetaTotal = chkPrecioManual.Checked ? GetPrecioManualTotalActual() : CalcularTotalConIva(costoTotalReceta);
                precioPublicoRecetaBase = chkPrecioManual.Checked ? ExtraerBaseDesdeTotal(precioPublicoRecetaTotal) : costoTotalReceta;
                SaeCatalogAdmin.UpsertPrecioPublico(con, tx, _cveArtActual, precioPublicoRecetaBase, 1);
                SaeCatalogAdmin.ApplyNativeInveDefaults(con, tx, _cveArtActual, esKit: true);

                tx.Commit();

                DataChanged = true;
                ChangedCveArt = _cveArtActual;
                ChangedPrecioPublico = precioPublicoRecetaBase;

                if (this.Owner is Form1 main)
                    main.RefrescarCatalogosEnPantalla(ChangedCveArt, ChangedPrecioPublico);

                var ivaGuardado = chkPrecioManual.Checked ? CalcularIvaDesdeTotal(precioPublicoRecetaTotal) : CalcularIva(costoTotalReceta);
                MessageBox.Show($"Receta (KITS) guardada en SAE correctamente.\nCosto actualizado: {costoTotalReceta:N2}\nIVA receta: {ivaGuardado:N2}\nPrecio público total: {precioPublicoRecetaTotal:N2}\nPrecio base guardado en SAE: {precioPublicoRecetaBase:N2}");
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                MessageBox.Show(
                    "No se pudo guardar la receta en SAE.\n\n" + ex.Message,
                    "Error al guardar receta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ========= Grid recalculos =========

        private void dgvIngredientes_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvIngredientes.Rows[e.RowIndex];

            var colName = dgvIngredientes.Columns[e.ColumnIndex].Name;

            if (colName == "colIngrediente")
            {
                var cveIng = row.Cells["colIngrediente"].Value?.ToString();
                CompletarUnidadYCostoDesdeCatalogo(row, cveIng ?? "");
            }

            // Si cambia algo que impacta el costo, recalcular
            if (colName == "colCantidad" ||
                colName == "colCostoUnit" || colName == "colIngrediente")
            {
                RecalcFila(row);
                RecalcTotales();
                MarcarCambiosSiAplica();
            }


        }


        private void dgvIngredientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvIngredientes.Columns[e.ColumnIndex].Name == "colEliminar")
            {
                dgvIngredientes.Rows.RemoveAt(e.RowIndex);
                RecalcTotales();
                MarcarCambiosSiAplica();
            }
        }

        private void RecalcFila(DataGridViewRow row)
        {
            decimal cantCaptura = ToDec(row.Cells["colCantidad"].Value);
            decimal costoBase = ToDec(row.Cells["colCostoUnit"].Value);
            var meta = GetIngredienteMeta(row.Cells["colIngrediente"].Value?.ToString());
            decimal factor = meta.FacConv <= 0m ? 1m : meta.FacConv;
            decimal cantidadBase = cantCaptura;
            row.Cells["colCostoTotal"].Value = Math.Round(cantidadBase * costoBase, 4);
        }


        private void RecalcTotales()
        {
            decimal total = 0m;
            foreach (DataGridViewRow r in dgvIngredientes.Rows)
            {
                if (r.IsNewRow) continue;
                total += ToDec(r.Cells["colCostoTotal"].Value);
            }
            lblTotal.Text = $"Costo total receta: {total:N2}";
            ActualizarIvaVisual(total);
            if (nudPrecioPublico != null && !chkPrecioManual.Checked)
                SyncPrecioConCosto(total);
        }


        private (string UnidadBase, string UnidadCaptura, decimal FacConv, decimal CostoBase, decimal Existencia) GetIngredienteMeta(string? cveIng)
        {
            if (string.IsNullOrWhiteSpace(cveIng) || _dtIngredientes == null)
                return ("pz", "pz", 1m, 0m, 0m);

            var found = _dtIngredientes.Select($"CLAVE = '{cveIng.Replace("'", "''")}'");
            if (found.Length == 0)
                return ("pz", "pz", 1m, 0m, 0m);

            var baseUnit = found[0]["UNIDAD_BASE"]?.ToString() ?? "pz";
            var captureUnit = found[0]["UNIDAD_CAPTURA"]?.ToString() ?? baseUnit;
            var facConv = ToDec(found[0]["FAC_CONV"]);
            if (facConv <= 0m) facConv = 1m;
            var costoBase = ToDec(found[0]["COSTO"]);
            var existencia = ToDec(found[0]["EXISTENCIA"]);
            return (baseUnit, captureUnit, facConv, costoBase, existencia);
        }

        private void CompletarUnidadYCostoDesdeCatalogo(DataGridViewRow row, string cveIng)
        {
            if (string.IsNullOrWhiteSpace(cveIng)) return;

            var meta = GetIngredienteMeta(cveIng);
            row.Cells["colUnidad"].Value = meta.UnidadCaptura;

            decimal costoActual = 0m;
            decimal.TryParse(row.Cells["colCostoUnit"].Value?.ToString(), out costoActual);
            if (costoActual == 0m && meta.CostoBase > 0m)
                row.Cells["colCostoUnit"].Value = meta.CostoBase;

            MostrarAvisoIngredienteSinExistenciaOCosto(cveIng, meta.Existencia, meta.CostoBase);
        }

        private void MostrarAvisoIngredienteSinExistenciaOCosto(string cveIng, decimal existencia, decimal costoBase)
        {
            if (_suppressIngredientWarnings)
                return;

            bool sinExistencia = existencia <= 0m;
            bool sinCosto = costoBase <= 0m;
            if (!sinExistencia && !sinCosto)
                return;

            var found = _dtIngredientes?.Select($"CLAVE = '{cveIng.Replace("'", "''")}'");
            var nombre = found != null && found.Length > 0
                ? found[0]["NOMBRE"]?.ToString()?.Trim()
                : cveIng;

            var detalle = sinExistencia && sinCosto
                ? "no tiene existencias ni costo registrado"
                : sinExistencia
                    ? "no tiene existencias registradas"
                    : "no tiene costo registrado";

            MessageBox.Show(
                $"El insumo '{nombre}' {detalle}.\n\n" +
                "Es preferible agregar insumos que ya tengan existencia o costo, para que el sistema pueda sugerir mejor el precio de la receta.\n\n" +
                "Aun así, puedes dejarlo en la receta y guardar el registro si así lo necesitas.",
                "Aviso del insumo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static decimal CalcularIva(decimal baseNeta)
            => decimal.Round(baseNeta * TASA_IVA, 2);

        private static decimal CalcularTotalConIva(decimal baseNeta)
            => decimal.Round(baseNeta + CalcularIva(baseNeta), 2);

        private static decimal ExtraerBaseDesdeTotal(decimal totalConIva)
        {
            if (totalConIva <= 0m) return 0m;
            return decimal.Round(totalConIva / (1m + TASA_IVA), 2);
        }

        private static decimal CalcularIvaDesdeTotal(decimal totalConIva)
        {
            if (totalConIva <= 0m) return 0m;
            return decimal.Round(totalConIva - ExtraerBaseDesdeTotal(totalConIva), 2);
        }

        private decimal GetPrecioManualTotalActual()
        {
            if (nudPrecioPublico == null)
                return 0m;

            var txt = (nudPrecioPublico.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(txt))
                return nudPrecioPublico.Value;

            if (decimal.TryParse(txt, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
                return decimal.Round(parsed, 2);

            var normalized = txt.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol, string.Empty).Trim();
            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
                return decimal.Round(parsed, 2);

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
                return decimal.Round(parsed, 2);

            return nudPrecioPublico.Value;
        }

        private void ActualizarIvaVisual(decimal? costoReceta = null)
        {
            var costoBase = costoReceta ?? GetCostoTotalActual();
            var ivaMostrado = chkPrecioManual.Checked
                ? CalcularIvaDesdeTotal(GetPrecioManualTotalActual())
                : CalcularIva(costoBase);

            if (lblIvaTotal != null)
                lblIvaTotal.Text = $"IVA total receta: ${ivaMostrado:N2}";
        }

        private void SyncPrecioConCosto(decimal? total = null)
        {
            if (nudPrecioPublico == null) return;
            var value = CalcularTotalConIva(total ?? GetCostoTotalActual());
            if (value < nudPrecioPublico.Minimum) value = nudPrecioPublico.Minimum;
            if (value > nudPrecioPublico.Maximum) value = nudPrecioPublico.Maximum;
            nudPrecioPublico.Value = decimal.Round(value, 2);
            ActualizarIvaVisual(total);
        }

        private decimal GetCostoTotalActual()
        {
            decimal total = 0m;
            foreach (DataGridViewRow r in dgvIngredientes.Rows)
            {
                if (r.IsNewRow) continue;
                total += ToDec(r.Cells["colCostoTotal"].Value);
            }
            return total;
        }

        private void CargarPrecioDesdeSae()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_cveArtActual))
                {
                    chkPrecioManual.Checked = false;
                    SyncPrecioConCosto();
                    return;
                }

                using var con = SaeDb.GetOpenConnection();
                string tPXP = SaeDb.GetTableName(con, "PRECIO_X_PROD");
                using var cmd = new FbCommand($@"SELECT COALESCE(PRECIO, 0) FROM {tPXP} WHERE CVE_ART = @C AND CVE_PRECIO = 1", con);
                cmd.Parameters.Add("@C", FbDbType.VarChar, 16).Value = _cveArtActual;
                var oPrecio = cmd.ExecuteScalar();
                var precioActual = oPrecio == null || oPrecio == DBNull.Value ? 0m : Convert.ToDecimal(oPrecio);
                var costoActual = GetCostoTotalActual();

                bool manual = decimal.Round(precioActual, 2) != decimal.Round(costoActual, 2) && precioActual > 0m;
                chkPrecioManual.Checked = manual;
                nudPrecioPublico.Enabled = manual;
                if (manual)
                {
                    if (precioActual < nudPrecioPublico.Minimum) precioActual = nudPrecioPublico.Minimum;
                    if (precioActual > nudPrecioPublico.Maximum) precioActual = nudPrecioPublico.Maximum;
                    nudPrecioPublico.Value = decimal.Round(CalcularTotalConIva(precioActual), 2);
                }
                else
                {
                    SyncPrecioConCosto(costoActual);
                }
            }
            catch
            {
                chkPrecioManual.Checked = false;
                if (nudPrecioPublico != null) nudPrecioPublico.Enabled = false;
                SyncPrecioConCosto();
            }
        }


        private static decimal ToDec(object v) =>
            v == null || v == DBNull.Value || string.IsNullOrWhiteSpace(v.ToString())
                ? 0m : Convert.ToDecimal(v);



        //no borrar, o explota la interfaz
        private void FormRecetaEditor_Load(object sender, EventArgs e)
        {

        }
        //hasta aqui lo que no se borra


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            { btnGuardar.PerformClick(); return true; }

            if (keyData == (Keys.Control | Keys.N))
            { btnNuevoPlatillo.PerformClick(); return true; }

            if (keyData == Keys.Insert)
            { btnAgregarFila.PerformClick(); return true; }


            if (keyData == (Keys.Control | Keys.Delete))
            { btnEliminarTodo.PerformClick(); return true; }

            if (keyData == Keys.Delete && dgvIngredientes.Focused)
            { btnEliminarProducto.PerformClick(); return true; }

            if (keyData == (Keys.Control | Keys.F))
            { cboPlatillo.Focus(); return true; }

            if (keyData == Keys.Escape)
            { Close(); return true; }

            return base.ProcessCmdKey(ref msg, keyData);
        }

    }
}