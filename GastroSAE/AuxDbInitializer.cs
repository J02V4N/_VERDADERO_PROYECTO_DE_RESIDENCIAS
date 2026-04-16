using FirebirdSql.Data.FirebirdClient;

namespace GastroSAE
{
    public static class AuxDbInitializer
    {
        public static string GetDefaultAuxDbPath()
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GastroSAE",
                "Data");

            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "RESTAURANTE.FDB");
        }

        public static string GetLegacyAuxDbPath()
        {
#if DEBUG
            var appRoot = ResolveAppRoot();
            if (string.IsNullOrWhiteSpace(appRoot))
                appRoot = AppContext.BaseDirectory;
#else
            var appRoot = AppContext.BaseDirectory;
#endif
            return Path.Combine(appRoot, "RESTAURANTE.FDB");
        }

#if DEBUG
        private static string ResolveAppRoot()
        {
            var dir = AppContext.BaseDirectory;
            try
            {
                var d = new DirectoryInfo(dir);
                for (int i = 0; i < 5 && d != null; i++, d = d.Parent)
                {
                    bool hasCsproj = Directory.EnumerateFiles(d.FullName, "*.csproj", SearchOption.TopDirectoryOnly).Any();
                    if (hasCsproj)
                        return d.FullName;
                }
            }
            catch { }
            return string.Empty;
        }
#endif

        private static void TryMigrateLegacyDatabase(string legacyPath, string newPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(legacyPath) || string.IsNullOrWhiteSpace(newPath))
                    return;
                if (string.Equals(Path.GetFullPath(legacyPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
                    return;
                if (File.Exists(newPath) || !File.Exists(legacyPath))
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
                File.Move(legacyPath, newPath);
            }
            catch
            {
                // Si no se puede mover, la app seguirá usando/creando la base en la ruta nueva.
            }
        }

        public static FbConnection EnsureCreated(
            out string dbPath,
            string server = "127.0.0.1",
            int port = 3050,
            string user = "SYSDBA",
            string password = "masterkey",
            string charset = "ISO8859_1")
        {
            dbPath = GetDefaultAuxDbPath();
            var legacyPath = GetLegacyAuxDbPath();
            TryMigrateLegacyDatabase(legacyPath, dbPath);

            var csb = new FbConnectionStringBuilder
            {
                DataSource = server,
                Port = port,
                Database = dbPath,
                UserID = user,
                Password = password,
                Charset = charset,
                Dialect = 3,
                Pooling = true
            };

            if (!File.Exists(dbPath))
            {
                FbConnection.CreateDatabase(csb.ToString(), 8192, false);
                var connNew = new FbConnection(csb.ToString());
                connNew.Open();
                CreateSlimSchema(connNew);
                SeedConfig(connNew);
                return connNew;
            }

            var conn = new FbConnection(csb.ToString());
            conn.Open();

            if (!TableExists(conn, "CONFIG"))
            {
                CreateSlimSchema(conn);
                SeedConfig(conn);
                return conn;
            }

            if (HasLegacyOperationalSchema(conn))
            {
                var export = ExportSlimData(conn);
                conn.Close();
                conn.Dispose();

                var backupPath = dbPath + ".legacy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(dbPath, backupPath);

                FbConnection.CreateDatabase(csb.ToString(), 8192, false);
                var connSlim = new FbConnection(csb.ToString());
                connSlim.Open();
                CreateSlimSchema(connSlim);
                SeedConfig(connSlim);
                ImportSlimData(connSlim, export);
                return connSlim;
            }

            EnsureSlimObjects(conn);
            return conn;
        }

        public static void UpsertConfig(FbConnection conn, string clave, string valor)
        {
            using (var upd = new FbCommand("UPDATE CONFIG SET VALOR=@VALOR WHERE CLAVE=@CLAVE", conn))
            {
                upd.Parameters.Add(new FbParameter("@VALOR", FbDbType.VarChar, 255) { Value = valor ?? string.Empty });
                upd.Parameters.Add(new FbParameter("@CLAVE", FbDbType.VarChar, 50) { Value = clave });
                if (upd.ExecuteNonQuery() > 0) return;
            }

            using var ins = new FbCommand("INSERT INTO CONFIG (CLAVE, VALOR) VALUES (@CLAVE, @VALOR)", conn);
            ins.Parameters.Add(new FbParameter("@CLAVE", FbDbType.VarChar, 50) { Value = clave });
            ins.Parameters.Add(new FbParameter("@VALOR", FbDbType.VarChar, 255) { Value = valor ?? string.Empty });
            ins.ExecuteNonQuery();
        }

        public static string? GetConfig(FbConnection conn, string clave)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (string.IsNullOrWhiteSpace(clave)) throw new ArgumentException("La clave no puede ser vacía.", nameof(clave));

            using var cmd = new FbCommand("SELECT VALOR FROM CONFIG WHERE CLAVE=@CLAVE", conn);
            cmd.Parameters.Add(new FbParameter("@CLAVE", FbDbType.VarChar, 50) { Value = clave });
            var o = cmd.ExecuteScalar();
            return o?.ToString();
        }

        public static string? GetConfig(string clave)
        {
            string path;
            using var conn = EnsureCreated(out path, charset: "ISO8859_1");
            return GetConfig(conn, clave);
        }

        private static bool TableExists(FbConnection conn, string tableNameUpper, FbTransaction? tx = null)
        {
            using var cmd = tx == null
                ? new FbCommand(@"
SELECT 1
FROM RDB$RELATIONS
WHERE RDB$SYSTEM_FLAG = 0
  AND RDB$RELATION_NAME = @T", conn)
                : new FbCommand(@"
SELECT 1
FROM RDB$RELATIONS
WHERE RDB$SYSTEM_FLAG = 0
  AND RDB$RELATION_NAME = @T", conn, tx);
            cmd.Parameters.Add(new FbParameter("@T", FbDbType.Char, 31) { Value = tableNameUpper.ToUpperInvariant() });
            var o = cmd.ExecuteScalar();
            return o != null && o != DBNull.Value;
        }

        private static bool SequenceExists(FbConnection conn, string sequenceUpper, FbTransaction? tx = null)
        {
            using var cmd = tx == null
                ? new FbCommand(@"
SELECT 1
FROM RDB$GENERATORS
WHERE RDB$GENERATOR_NAME = @G", conn)
                : new FbCommand(@"
SELECT 1
FROM RDB$GENERATORS
WHERE RDB$GENERATOR_NAME = @G", conn, tx);
            cmd.Parameters.Add(new FbParameter("@G", FbDbType.Char, 31) { Value = sequenceUpper.ToUpperInvariant() });
            var o = cmd.ExecuteScalar();
            return o != null && o != DBNull.Value;
        }

        private static bool TriggerExists(FbConnection conn, string triggerUpper, FbTransaction? tx = null)
        {
            using var cmd = tx == null
                ? new FbCommand(@"
SELECT 1
FROM RDB$TRIGGERS
WHERE RDB$TRIGGER_NAME = @T", conn)
                : new FbCommand(@"
SELECT 1
FROM RDB$TRIGGERS
WHERE RDB$TRIGGER_NAME = @T", conn, tx);
            cmd.Parameters.Add(new FbParameter("@T", FbDbType.Char, 31) { Value = triggerUpper.ToUpperInvariant() });
            var o = cmd.ExecuteScalar();
            return o != null && o != DBNull.Value;
        }

        private static bool HasLegacyOperationalSchema(FbConnection conn)
        {
            return TableExists(conn, "TURNOS")
                || TableExists(conn, "MESA_TURNO")
                || TableExists(conn, "PEDIDOS")
                || TableExists(conn, "PEDIDO_DET")
                || TableExists(conn, "RECETAS")
                || TableExists(conn, "RECETA_DET")
                || TableExists(conn, "INSUMO_EXT")
                || TableExists(conn, "BASCULA_LECTURAS")
                || TableExists(conn, "MOV_INV_AUX")
                || TableExists(conn, "USUARIOS");
        }

        private static void EnsureSlimObjects(FbConnection conn)
        {
            using var tx = conn.BeginTransaction();
            void Exec(string sql)
            {
                using var cmd = new FbCommand(sql, conn, tx);
                cmd.ExecuteNonQuery();
            }

            if (!SequenceExists(conn, "GEN_MESAS_ID", tx)) Exec("CREATE SEQUENCE GEN_MESAS_ID;");
            if (!SequenceExists(conn, "GEN_MESEROS_ID", tx)) Exec("CREATE SEQUENCE GEN_MESEROS_ID;");
            if (!TableExists(conn, "MESAS", tx))
            {
                Exec(@"CREATE TABLE MESAS (
  ID_MESA    INTEGER NOT NULL PRIMARY KEY,
  NOMBRE     VARCHAR(30) NOT NULL,
  CAPACIDAD  SMALLINT,
  ESTADO     VARCHAR(12) NOT NULL
);");
            }
            if (!TableExists(conn, "MESEROS", tx))
            {
                Exec(@"CREATE TABLE MESEROS (
  ID_MESERO  INTEGER NOT NULL PRIMARY KEY,
  NOMBRE     VARCHAR(60) NOT NULL,
  ACTIVO     SMALLINT NOT NULL
);");
            }
            if (!TableExists(conn, "CONFIG", tx))
            {
                Exec(@"CREATE TABLE CONFIG (
  CLAVE VARCHAR(50) NOT NULL PRIMARY KEY,
  VALOR VARCHAR(255) NOT NULL
);");
            }

            if (!TriggerExists(conn, "BI_MESAS", tx))
            {
                Exec(@"CREATE TRIGGER BI_MESAS FOR MESAS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESA IS NULL) THEN NEW.ID_MESA = GEN_ID(GEN_MESAS_ID, 1);
  IF (NEW.ESTADO IS NULL)  THEN NEW.ESTADO = 'LIBRE';
END;");
            }

            if (!TriggerExists(conn, "BI_MESEROS", tx))
            {
                Exec(@"CREATE TRIGGER BI_MESEROS FOR MESEROS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESERO IS NULL) THEN NEW.ID_MESERO = GEN_ID(GEN_MESEROS_ID, 1);
  IF (NEW.ACTIVO IS NULL)    THEN NEW.ACTIVO = 1;
END;");
            }

            tx.Commit();
            SeedConfig(conn);
        }

        private static void CreateSlimSchema(FbConnection conn)
        {
            using var tx = conn.BeginTransaction();
            void Exec(string sql)
            {
                using var cmd = new FbCommand(sql, conn, tx);
                cmd.ExecuteNonQuery();
            }

            Exec("CREATE SEQUENCE GEN_MESAS_ID;");
            Exec("CREATE SEQUENCE GEN_MESEROS_ID;");

            Exec(@"CREATE TABLE MESAS (
  ID_MESA    INTEGER NOT NULL PRIMARY KEY,
  NOMBRE     VARCHAR(30) NOT NULL,
  CAPACIDAD  SMALLINT,
  ESTADO     VARCHAR(12) NOT NULL
);");

            Exec(@"CREATE TABLE MESEROS (
  ID_MESERO  INTEGER NOT NULL PRIMARY KEY,
  NOMBRE     VARCHAR(60) NOT NULL,
  ACTIVO     SMALLINT NOT NULL
);");

            Exec(@"CREATE TABLE CONFIG (
  CLAVE VARCHAR(50) NOT NULL PRIMARY KEY,
  VALOR VARCHAR(255) NOT NULL
);");

            Exec(@"CREATE TRIGGER BI_MESAS FOR MESAS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESA IS NULL) THEN NEW.ID_MESA = GEN_ID(GEN_MESAS_ID, 1);
  IF (NEW.ESTADO IS NULL)  THEN NEW.ESTADO = 'LIBRE';
END;");

            Exec(@"CREATE TRIGGER BI_MESEROS FOR MESEROS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESERO IS NULL) THEN NEW.ID_MESERO = GEN_ID(GEN_MESEROS_ID, 1);
  IF (NEW.ACTIVO IS NULL)    THEN NEW.ACTIVO = 1;
END;");

            Exec("ALTER TABLE MESEROS ADD CONSTRAINT CK_MES_ACTIVO CHECK (ACTIVO IN (0,1));");
            tx.Commit();
        }

        private static void SeedConfig(FbConnection conn)
        {
            var seed = new (string Clave, string Valor)[]
            {
                ("ALMACEN_DEFAULT", "1"),
                ("IMPUESTO_PORC", "16"),
                ("IMPRESORA_TICKET", ""),
                ("SAE_FDB", ""),
                ("BASCULA_PUERTO", "COM1"),
                ("LISTA_PRECIOS", "1"),
                ("NEGOCIO_NOMBRE", ""),
                ("TICKET_ANCHO_MM", "58")
            };

            foreach (var kv in seed)
                if (GetConfig(conn, kv.Clave) == null)
                    UpsertConfig(conn, kv.Clave, kv.Valor);
        }

        private sealed class SlimExport
        {
            public List<(string Clave, string Valor)> Config { get; } = new();
            public List<(int IdMesa, string Nombre, int? Capacidad, string Estado)> Mesas { get; } = new();
            public List<(int IdMesero, string Nombre, short Activo)> Meseros { get; } = new();
        }

        private static SlimExport ExportSlimData(FbConnection conn)
        {
            var export = new SlimExport();

            if (TableExists(conn, "CONFIG"))
            {
                using var cmd = new FbCommand("SELECT CLAVE, VALOR FROM CONFIG", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    export.Config.Add((rd[0]?.ToString() ?? string.Empty, rd[1]?.ToString() ?? string.Empty));
            }

            if (TableExists(conn, "MESAS"))
            {
                using var cmd = new FbCommand("SELECT ID_MESA, NOMBRE, CAPACIDAD, ESTADO FROM MESAS", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    export.Mesas.Add((Convert.ToInt32(rd[0]), rd[1]?.ToString() ?? string.Empty, rd[2] == DBNull.Value ? (int?)null : Convert.ToInt32(rd[2]), rd[3]?.ToString() ?? "LIBRE"));
            }

            if (TableExists(conn, "MESEROS"))
            {
                using var cmd = new FbCommand("SELECT ID_MESERO, NOMBRE, ACTIVO FROM MESEROS", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    export.Meseros.Add((Convert.ToInt32(rd[0]), rd[1]?.ToString() ?? string.Empty, Convert.ToInt16(rd[2])));
            }

            return export;
        }

        private static void ImportSlimData(FbConnection conn, SlimExport export)
        {
            using var tx = conn.BeginTransaction();

            foreach (var (clave, valor) in export.Config)
            {
                using var cmd = new FbCommand("UPDATE OR INSERT INTO CONFIG (CLAVE, VALOR) VALUES (@C, @V) MATCHING (CLAVE)", conn, tx);
                cmd.Parameters.Add("@C", FbDbType.VarChar, 50).Value = clave;
                cmd.Parameters.Add("@V", FbDbType.VarChar, 255).Value = valor;
                cmd.ExecuteNonQuery();
            }

            foreach (var (idMesa, nombre, capacidad, estado) in export.Mesas)
            {
                using var cmd = new FbCommand("INSERT INTO MESAS (ID_MESA, NOMBRE, CAPACIDAD, ESTADO) VALUES (@ID, @N, @C, @E)", conn, tx);
                cmd.Parameters.Add("@ID", FbDbType.Integer).Value = idMesa;
                cmd.Parameters.Add("@N", FbDbType.VarChar, 30).Value = nombre;
                cmd.Parameters.Add("@C", FbDbType.Integer).Value = (object?)capacidad ?? DBNull.Value;
                cmd.Parameters.Add("@E", FbDbType.VarChar, 12).Value = string.IsNullOrWhiteSpace(estado) ? "LIBRE" : estado;
                cmd.ExecuteNonQuery();
            }

            foreach (var (idMesero, nombre, activo) in export.Meseros)
            {
                using var cmd = new FbCommand("INSERT INTO MESEROS (ID_MESERO, NOMBRE, ACTIVO) VALUES (@ID, @N, @A)", conn, tx);
                cmd.Parameters.Add("@ID", FbDbType.Integer).Value = idMesero;
                cmd.Parameters.Add("@N", FbDbType.VarChar, 60).Value = nombre;
                cmd.Parameters.Add("@A", FbDbType.SmallInt).Value = activo;
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            AjustarSecuencias(conn);
        }

        private static void AjustarSecuencias(FbConnection conn)
        {
            int maxMesa = 0, maxMesero = 0;
            using (var cmd = new FbCommand("SELECT COALESCE(MAX(ID_MESA),0) FROM MESAS", conn))
                maxMesa = Convert.ToInt32(cmd.ExecuteScalar());
            using (var cmd = new FbCommand("SELECT COALESCE(MAX(ID_MESERO),0) FROM MESEROS", conn))
                maxMesero = Convert.ToInt32(cmd.ExecuteScalar());

            using (var cmd = new FbCommand($"SET GENERATOR GEN_MESAS_ID TO {maxMesa};", conn))
                cmd.ExecuteNonQuery();
            using (var cmd = new FbCommand($"SET GENERATOR GEN_MESEROS_ID TO {maxMesero};", conn))
                cmd.ExecuteNonQuery();
        }
    }
}
