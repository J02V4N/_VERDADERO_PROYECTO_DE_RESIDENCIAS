using FirebirdSql.Data.FirebirdClient;
using System.Data;

namespace PROYECTO_RESIDENCIAS
{
    public partial class FormRecetaEditor : Form
    {
        private DataTable _dtPlatillos;   // de SAE (clave/desc/unidad/precio)
        private DataTable _dtIngredientes;// de SAE (usaremos INVE como catálogo de “ingredientes”)
        private string _cveArtActual;     // CVE_ART del platillo elegido

        public FormRecetaEditor()
        {
            InitializeComponent();
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
            btnReenumerar.Click += btnReenumerar_Click;

            dgvIngredientes.CellValueChanged += dgvIngredientes_CellValueChanged;
            dgvIngredientes.CellContentClick += dgvIngredientes_CellContentClick;
            dgvIngredientes.RowsAdded += (_, __) => RecalcTotales();
            dgvIngredientes.RowsRemoved += (_, __) => RecalcTotales();

            if (cboPlatillo.Items.Count > 0) cboPlatillo.SelectedIndex = 0;
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
SELECT CVE_ART, DESCR, UNI_MED, COSTO_PROM
FROM {tINVE}
WHERE STATUS IS NULL OR STATUS <> 'B'
ORDER BY DESCR", con);

            using var rd = cmd.ExecuteReader();
            _dtIngredientes = new DataTable();
            _dtIngredientes.Columns.Add("CLAVE", typeof(string));
            _dtIngredientes.Columns.Add("NOMBRE", typeof(string));
            _dtIngredientes.Columns.Add("UNIDAD", typeof(string));
            _dtIngredientes.Columns.Add("COSTO", typeof(decimal));
            while (rd.Read())
            {
                _dtIngredientes.Rows.Add(
                    rd.IsDBNull(0) ? "" : rd.GetString(0).Trim(),
                    rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                    rd.IsDBNull(2) ? "" : rd.GetString(2).Trim(),
                    rd.IsDBNull(3) ? 0m : Convert.ToDecimal(rd.GetValue(3)));
            }
        }

        // ========= GRID & UI =========

        private void PrepararGrid()
        {
            dgvIngredientes.AutoGenerateColumns = false;
            dgvIngredientes.Columns.Clear();

            // (Opcional) Línea solo para ordenar en UI; NO se guarda en SAE
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLinea", HeaderText = "Línea", Width = 60 });

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

            // SAE -> KITS.PORCEN
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPorcen", HeaderText = "% Proporción", Width = 100 });

            // Costo referencial (INVE.COSTO_PROM)
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCostoUnit", HeaderText = "Costo Unit", ReadOnly = true, Width = 90 });
            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCostoTotal", HeaderText = "Costo Total", ReadOnly = true, Width = 110 });

            dgvIngredientes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotas", HeaderText = "Notas (solo UI)", Width = 200 });

            dgvIngredientes.Columns.Add(new DataGridViewButtonColumn { Name = "colEliminar", HeaderText = "", Text = "X", UseColumnTextForButtonValue = true, Width = 40 });
        }


        private void CargarRecetaDelPlatillo()
        {
            if (cboPlatillo.SelectedValue == null) return;
            _cveArtActual = cboPlatillo.SelectedValue.ToString();

            dgvIngredientes.Rows.Clear();

            using var con = SaeDb.GetOpenConnection();
            string tKITS = SaeDb.GetTableName(con, "KITS"); // KITS01..KITS99
            string tINVE = SaeDb.GetTableName(con, "INVE");

            var sql = $@"
SELECT
  K.CVE_PROD,                          -- 0 ingrediente
  I.DESCR,                             -- 1 nombre
  I.UNI_MED,                           -- 2 unidad
  COALESCE(K.PORCEN, 0),               -- 3 % proporción
  COALESCE(K.CANTIDAD, 0),             -- 4 cantidad
  COALESCE(I.COSTO_PROM, 0)            -- 5 costo unit
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
                row.Cells["colUnidad"].Value = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim();
                row.Cells["colPorcen"].Value = rd.IsDBNull(3) ? 0m : Convert.ToDecimal(rd.GetValue(3));
                row.Cells["colCantidad"].Value = rd.IsDBNull(4) ? 0m : Convert.ToDecimal(rd.GetValue(4));
                row.Cells["colCostoUnit"].Value = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5));

                // Línea solo UI: autoincremento (10,20,30…)
                var max = 0;
                foreach (DataGridViewRow r in dgvIngredientes.Rows)
                    max = Math.Max(max, Convert.ToInt32(r.Cells["colLinea"].Value ?? 0));
                row.Cells["colLinea"].Value = max + 10;

                RecalcFila(row);
            }

            RecalcTotales();
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
            int linea;
            if (chkAutoEnumerar.Checked)
            {
                var max = 0;
                foreach (DataGridViewRow r in dgvIngredientes.Rows)
                    max = Math.Max(max, Convert.ToInt32(r.Cells["colLinea"].Value ?? 0));
                linea = max + 10;
            }
            else
            {
                linea = (int)nudLinea.Value;
                foreach (DataGridViewRow r in dgvIngredientes.Rows)
                    if (Convert.ToInt32(r.Cells["colLinea"].Value ?? -1) == linea)
                    { MessageBox.Show("Esa línea ya existe."); return; }
            }

            int idx = dgvIngredientes.Rows.Add();
            var row = dgvIngredientes.Rows[idx];
            row.Cells["colLinea"].Value = linea;
            row.Cells["colPorcen"].Value = 0m;      // <-- antes colMerma
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

                // 3) BORRADO FÍSICO (NO recomendado salvo que estés seguro)
                using (var cmdDelInv = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $@"DELETE FROM {tINVE} WHERE CVE_ART=@C", con, tx))
                {
                    cmdDelInv.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = _cveArtActual;
                    var rows = cmdDelInv.ExecuteNonQuery();
                    if (rows == 0)
                        throw new Exception("No se encontró el producto en INVE para borrar.");
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
            var um = (f.Unidad ?? "PZA").Trim();

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

                // 2) INSERT mínimo como KIT (TIPO_ELE='K'), STATUS NULL = activo
                var sql = $@"
INSERT INTO {tINVE} (CVE_ART, DESCR, UNI_MED, TIPO_ELE, STATUS)
VALUES (@CVE_ART, @DESCR, @UNI_MED, @TIPO_ELE, NULL)";
                using (var cmdIns = new FirebirdSql.Data.FirebirdClient.FbCommand(sql, con, tx))
                {
                    cmdIns.Parameters.Add("@CVE_ART", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30).Value = cveArt;
                    cmdIns.Parameters.Add("@DESCR", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 120).Value = descr;
                    cmdIns.Parameters.Add("@UNI_MED", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 10).Value = um;
                    cmdIns.Parameters.Add("@TIPO_ELE", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 1).Value = "K";
                    cmdIns.ExecuteNonQuery();
                }

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
            foreach (DataGridViewRow r in dgvIngredientes.Rows)
            {
                if (r.IsNewRow) continue;
                if (r.Cells["colIngrediente"].Value == null || string.IsNullOrWhiteSpace(r.Cells["colIngrediente"].Value.ToString()))
                { MessageBox.Show("Hay filas sin ingrediente."); return; }
                if (!decimal.TryParse(r.Cells["colCantidad"].Value?.ToString(), out _))
                { MessageBox.Show("Hay filas sin cantidad válida."); return; }
                if (!decimal.TryParse(r.Cells["colPorcen"].Value?.ToString(), out _))
                { MessageBox.Show("Hay filas sin % proporción válido."); return; }
            }

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            string tKITS = SaeDb.GetTableName(con, "KITS", tx);  // con tx
                                                                 // ... comandos con (con, tx)


            // DELETE con tx
            using (var cmdDel = new FirebirdSql.Data.FirebirdClient.FbCommand(
                $@"DELETE FROM {tKITS} WHERE CVE_ART=@C", con, tx))
            {
                cmdDel.Parameters.Add("@C", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30)
                      .Value = _cveArtActual;
                cmdDel.ExecuteNonQuery();
            }

            // INSERT con tx
            var cmdIns = new FirebirdSql.Data.FirebirdClient.FbCommand(
                $@"INSERT INTO {tKITS} (CVE_ART, CVE_PROD, PORCEN, CANTIDAD)
       VALUES (@CVE_ART, @CVE_PROD, @PORCEN, @CANT)", con, tx);

            cmdIns.Parameters.Add("@CVE_ART", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30);
            cmdIns.Parameters.Add("@CVE_PROD", FirebirdSql.Data.FirebirdClient.FbDbType.VarChar, 30);
            cmdIns.Parameters.Add("@PORCEN", FirebirdSql.Data.FirebirdClient.FbDbType.Decimal);
            cmdIns.Parameters.Add("@CANT", FirebirdSql.Data.FirebirdClient.FbDbType.Decimal);

            foreach (DataGridViewRow r in dgvIngredientes.Rows)
            {
                if (r.IsNewRow) continue;

                cmdIns.Parameters["@CVE_ART"].Value = _cveArtActual;
                cmdIns.Parameters["@CVE_PROD"].Value = r.Cells["colIngrediente"].Value?.ToString() ?? "";
                cmdIns.Parameters["@PORCEN"].Value = ToDec(r.Cells["colPorcen"].Value);
                cmdIns.Parameters["@CANT"].Value = ToDec(r.Cells["colCantidad"].Value);

                cmdIns.ExecuteNonQuery();
            }

            tx.Commit();

            MessageBox.Show("Receta (KITS) guardada en SAE correctamente.");
        }


        private void btnReenumerar_Click(object sender, EventArgs e)
        {
            var rows = dgvIngredientes.Rows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .OrderBy(r => Convert.ToInt32(r.Cells["colLinea"].Value ?? 0))
                .ToList();
            int n = 10;
            foreach (var r in rows) { r.Cells["colLinea"].Value = n; n += 10; }
        }

        // ========= Grid recalculos =========

        private void dgvIngredientes_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvIngredientes.Rows[e.RowIndex];

            // ⬇️ Reemplaza TU bloque por este:
            if (dgvIngredientes.Columns[e.ColumnIndex].Name == "colIngrediente")
            {
                var cveIng = row.Cells["colIngrediente"].Value?.ToString();
                CompletarUnidadYCostoDesdeCatalogo(row, cveIng ?? "");
            }

            if (new[] { "colCantidad", "colPorcen", "colCostoUnit", "colIngrediente" }
    .Contains(dgvIngredientes.Columns[e.ColumnIndex].Name))
            {
                RecalcFila(row);
                RecalcTotales();
            }

        }


        private void dgvIngredientes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvIngredientes.Columns[e.ColumnIndex].Name == "colEliminar")
            {
                dgvIngredientes.Rows.RemoveAt(e.RowIndex);
                RecalcTotales();
            }
        }

        private void RecalcFila(DataGridViewRow row)
        {
            decimal cant = ToDec(row.Cells["colCantidad"].Value);
            decimal por = ToDec(row.Cells["colPorcen"].Value);   // <-- antes colMerma
            decimal costo = ToDec(row.Cells["colCostoUnit"].Value);

            // Si el % solo es informativo, no lo apliques:
            var cantEfectiva = cant; // o: cant * (1 + (por / 100m));
            row.Cells["colCostoTotal"].Value = Math.Round(cantEfectiva * costo, 4);
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
        }


        private void CompletarUnidadYCostoDesdeCatalogo(DataGridViewRow row, string cveIng)
        {
            if (string.IsNullOrWhiteSpace(cveIng)) return;

            var found = _dtIngredientes.Select($"CLAVE = '{cveIng.Replace("'", "''")}'");
            if (found.Length > 0)
            {
                // UM
                var um = found[0]["UNIDAD"]?.ToString();
                if (string.IsNullOrWhiteSpace(row.Cells["colUnidad"].Value?.ToString()) && !string.IsNullOrWhiteSpace(um))
                    row.Cells["colUnidad"].Value = um;

                // Costo (solo si está en 0 o vacío)
                decimal costoCat = 0m;
                if (decimal.TryParse(found[0]["COSTO"]?.ToString(), out var tmp)) costoCat = tmp;

                decimal costoActual = 0m;
                decimal.TryParse(row.Cells["colCostoUnit"].Value?.ToString(), out costoActual);

                if (costoActual == 0m && costoCat > 0m)
                    row.Cells["colCostoUnit"].Value = costoCat;
            }
        }


        private static decimal ToDec(object v) =>
            v == null || v == DBNull.Value || string.IsNullOrWhiteSpace(v.ToString())
                ? 0m : Convert.ToDecimal(v);
    }
}
