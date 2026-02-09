using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using FirebirdSql.Data.FirebirdClient;

namespace PROYECTO_RESIDENCIAS
{
    public static class SaeDb
    {
        private static string _cachedSaeSuffix;
        public static string ConnectionString { get; private set; }

        /// <summary>
        /// Limpia el sufijo de empresa cacheado (INVE##). Útil si cambias de empresa SAE en runtime.
        /// </summary>
        public static void ResetSaeSuffixCache() => _cachedSaeSuffix = null;

        /// <summary>
        /// Inicializa la cadena de conexión para toda la app.
        /// Llamar una sola vez al inicio (p.ej. desde Program/selección de empresa).
        /// </summary>
        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("ConnectionString no puede ser vacío.", nameof(connectionString));

            ConnectionString = connectionString;
            ResetSaeSuffixCache();
        }

        /// <summary>
        /// Devuelve una conexión nueva SIN abrir, usando la ConnectionString inicializada.
        /// </summary>
        public static FbConnection GetConnection()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("SaeDb no ha sido inicializado. Llama Initialize() primero o usa GetOpenConnection().");

            return new FbConnection(ConnectionString);
        }

        /// <summary>
        /// Construye una conexión NUEVA (sin abrir) a partir de parámetros sueltos.
        /// Úsala sólo cuando no quieras usar Initialize() aún.
        /// </summary>
        public static FbConnection CreateConnection(
            string databasePath,
            string server = "127.0.0.1",
            int port = 3050,
            string user = "SYSDBA",
            string password = "masterkey",
            string charset = "ISO8859_1")
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("databasePath no puede ser vacío.", nameof(databasePath));

            var csb = new FbConnectionStringBuilder
            {
                DataSource = server,
                Port = port,
                Database = databasePath,
                UserID = user,
                Password = password,
                Charset = charset,
                Dialect = 3,
                Pooling = true,
                // Si usas cliente embebido x86 local, mantén esto:
                ClientLibrary = "fbclient.dll"
            };

            return new FbConnection(csb.ToString());
        }

        /// <summary>
        /// Prueba rápida de conexión. 
        /// Abre la conexión si está cerrada y la vuelve a cerrar al terminar.
        /// </summary>
        public static void TestConnection(FbConnection conn)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));

            bool mustClose = conn.State != ConnectionState.Open;
            if (mustClose)
                conn.Open();

            try
            {
                using (var cmd = new FbCommand("SELECT 1 FROM RDB$DATABASE", conn))
                {
                    var o = cmd.ExecuteScalar();
                    if (Convert.ToInt32(o) != 1)
                        throw new Exception("Prueba SELECT 1 falló.");
                }

                // Prueba mínima contra una tabla típica de SAE 9 (INVE##)
                try
                {
                    var invTable = GetTableName(conn, "INVE"); // ← INVE##
                    using var cmd2 = new FbCommand(
                        $"SELECT FIRST 1 CVE_ART, DESCR FROM {invTable}",
                        conn);
                    using var _ = cmd2.ExecuteReader();
                    // ok si no truena
                }
                catch
                {
                    // Si no existe INVE##, no lo consideres fatal (ya probamos SELECT 1 antes)
                }
            }
            finally
            {
                if (mustClose)
                    conn.Close();
            }
        }

        /// <summary>
        /// Obtiene el sufijo de empresa de SAE (p.ej. "07" para tablas INVE07, KITS07).
        /// Resultado cacheado en memoria.
        /// </summary>
        public static string GetCompanySuffix(FbConnection con, FbTransaction tx = null)
        {
            if (!string.IsNullOrEmpty(_cachedSaeSuffix))
                return _cachedSaeSuffix;

            if (con == null) throw new ArgumentNullException(nameof(con));

            const string sql =
                "SELECT TRIM(RDB$RELATION_NAME) " +
                "FROM RDB$RELATIONS " +
                "WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL " +
                "AND RDB$RELATION_NAME STARTING WITH 'INVE'";

            using var cmd = tx == null
                ? new FbCommand(sql, con)
                : new FbCommand(sql, con, tx);

            using var rd = cmd.ExecuteReader();
            var posibles = new List<string>();

            while (rd.Read())
            {
                var name = rd.GetString(0).Trim();   // p.ej. INVE07
                var m = Regex.Match(name, @"^INVE(\d{2})$");
                if (m.Success)
                    posibles.Add(m.Groups[1].Value);
            }

            if (posibles.Count == 0)
                throw new Exception("No encontré ninguna tabla INVE## en esta BD. ¿Seguro es una BD de Aspel SAE?");

            // Normalmente habrá solo una; tomamos la primera
            _cachedSaeSuffix = posibles[0];
            return _cachedSaeSuffix;
        }

        public static string GetTableName(FbConnection con, string baseName)
            => GetTableName(con, baseName, null);

        /// <summary>
        /// Resuelve nombres reales con sufijo (p.ej. "INVE" -> "INVE01") consultando RDB$RELATIONS.
        /// Usa el sufijo de empresa detectado por GetCompanySuffix.
        /// </summary>
        public static string GetTableName(FbConnection con, string baseName, FbTransaction tx)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));
            if (string.IsNullOrWhiteSpace(baseName))
                throw new ArgumentException("baseName no puede ser vacío.", nameof(baseName));

            var sfx = GetCompanySuffix(con, tx);   // ej. "07"
            var candidato = baseName + sfx;        // ej. "INVE07"

            const string sqlCheck = @"
SELECT COUNT(*)
FROM RDB$RELATIONS
WHERE RDB$SYSTEM_FLAG = 0 AND TRIM(RDB$RELATION_NAME) = @t";

            using (var cmd = tx == null
                ? new FbCommand(sqlCheck, con)
                : new FbCommand(sqlCheck, con, tx))
            {
                cmd.Parameters.Add("@t", FbDbType.VarChar).Value = candidato;
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 1)
                    return candidato;
            }

            // Fallback: cualquier tabla que empiece por baseName y termine en dos dígitos
            const string sqlPref = @"
SELECT TRIM(RDB$RELATION_NAME)
FROM RDB$RELATIONS 
WHERE RDB$SYSTEM_FLAG=0 AND RDB$RELATION_NAME STARTING WITH @pref";

            using (var cmd2 = tx == null
                ? new FbCommand(sqlPref, con)
                : new FbCommand(sqlPref, con, tx))
            {
                cmd2.Parameters.Add("@pref", FbDbType.VarChar).Value = baseName;
                using var rd = cmd2.ExecuteReader();
                while (rd.Read())
                {
                    var name = rd.GetString(0).Trim();
                    if (Regex.IsMatch(name, "^" + Regex.Escape(baseName) + @"\d{2}$"))
                        return name;
                }
            }

            throw new Exception($"No encontré la tabla para base '{baseName}'.");
        }

        /// <summary>
        /// Devuelve una conexión ABIERTA.
        /// Si ya se inicializó ConnectionString, la usa.
        /// De lo contrario, lee CONFIG.SAE_FDB desde la BD Auxiliar.
        /// </summary>
        public static FbConnection GetOpenConnection()
        {
            // 1) Si ya inicializaste SaeDb.Initialize(connectionString)
            if (!string.IsNullOrWhiteSpace(ConnectionString))
            {
                var c = new FbConnection(ConnectionString);
                c.Open();
                return c;
            }

            // 2) Fallback: lee SAE_FDB de la BD Aux y arma la conexión
            string auxPath;
            using (var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1"))
            using (var cmd = new FbCommand("SELECT VALOR FROM CONFIG WHERE CLAVE='SAE_FDB'", aux))
            {
                var o = cmd.ExecuteScalar();
                var saePath = o?.ToString();
                if (string.IsNullOrWhiteSpace(saePath))
                    throw new InvalidOperationException("CONFIG.SAE_FDB está vacío. Selecciona la empresa de SAE primero.");

                var con = CreateConnection(
                    databasePath: saePath,
                    server: "127.0.0.1",
                    port: 3050,
                    user: "SYSDBA",
                    password: "masterkey",
                    charset: "ISO8859_1");
                con.Open();
                return con;
            }
        }

        /// <summary>
        /// Devuelve la descripción de un artículo en INVE##; si no existe, devuelve la clave.
        /// </summary>
        public static string ObtenerDescripcionArticulo(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new ArgumentException("La clave no puede ser vacía.", nameof(clave));

            using var con = GetOpenConnection();
            string tINVE = GetTableName(con, "INVE");

            using var cmd = new FbCommand($"SELECT DESCR FROM {tINVE} WHERE CVE_ART=@C", con);
            cmd.Parameters.Add("@C", FbDbType.VarChar, 30).Value = clave;
            var o = cmd.ExecuteScalar();
            return o?.ToString()?.Trim() ?? clave;
        }

        // ------------------------- Dominios: Platillos -------------------------

        public class PlatilloDto
        {
            public string Clave { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string Unidad { get; set; } = string.Empty;
            public decimal Precio { get; set; }
            public decimal Existencia { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        /// <summary>
        /// Lista platillos (artículos) desde SAE, con precio y existencia por almacén.
        /// </summary>
        public static List<PlatilloDto> ListarPlatillos(
            int listaPrecio = 1,
            int? almacen = 1,
            string clavePrefix = "Prep",   // por defecto, solo CVE_ART que comiencen con PREP
            string linProd = "Prep"        // opcional, por si luego filtras por línea
        )
        {
            using var conn = GetOpenConnection();

            string tINVE = GetTableName(conn, "INVE");
            string tPXP = GetTableName(conn, "PRECIO_X_PROD");
            string tMULT = GetTableName(conn, "MULT");

            // WHERE dinámico (case-insensitive para clave)
            var where = "WHERE I.STATUS = 'A' ";
            if (!string.IsNullOrWhiteSpace(clavePrefix))
                where += "AND UPPER(I.CVE_ART) STARTING WITH @PFX ";
            if (!string.IsNullOrWhiteSpace(linProd))
                where += "AND I.LIN_PROD = @LIN ";

            var sql = $@"
SELECT
    I.CVE_ART,
    I.DESCR,
    I.UNI_MED,
    I.STATUS,
    COALESCE(PX.PRECIO, 0) AS PRECIO,
    COALESCE(M.EXIST, I.EXIST) AS EXISTENCIA
FROM {tINVE} I
LEFT JOIN {tPXP} PX
       ON PX.CVE_ART = I.CVE_ART
      AND PX.CVE_PRECIO = @LISTA
LEFT JOIN {tMULT} M
       ON M.CVE_ART = I.CVE_ART
      AND (@ALM IS NULL OR M.CVE_ALM = @ALM)
{where}
ORDER BY I.DESCR";

            using var cmd = new FbCommand(sql, conn);
            cmd.Parameters.Add("@LISTA", FbDbType.Integer).Value = listaPrecio;
            cmd.Parameters.Add("@ALM", FbDbType.Integer).Value = (object?)almacen ?? DBNull.Value;

            if (!string.IsNullOrWhiteSpace(clavePrefix))
                cmd.Parameters.Add("@PFX", FbDbType.VarChar, 32).Value = clavePrefix.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(linProd))
                cmd.Parameters.Add("@LIN", FbDbType.VarChar, 20).Value = linProd;

            var list = new List<PlatilloDto>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new PlatilloDto
                {
                    Clave = rd.GetString(0).Trim(),
                    Descripcion = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                    Unidad = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim(),
                    Status = rd.IsDBNull(3) ? "" : rd.GetString(3).Trim(),
                    Precio = rd.IsDBNull(4) ? 0m : Convert.ToDecimal(rd.GetValue(4)),
                    Existencia = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5))
                });
            }

            return list;
        }

        // ------------------------- Dominios: Recetas (KITS) -------------------------

        public class RecetaItemDto
        {
            public string Clave { get; set; }          // CVE_PROD (ingrediente)
            public string Descripcion { get; set; }    // INVE.DESCR
            public string Unidad { get; set; }         // INVE.UNI_MED
            public decimal Porcentaje { get; set; }    // KITS.PORCEN
            public decimal Cantidad { get; set; }      // KITS.CANTIDAD (por 1 platillo)
            public decimal Existencia { get; set; }    // MULT.EXIST (almacén seleccionado)
        }

        public static List<RecetaItemDto> ListarReceta(string cvePlatillo, int? almacen = 1)
        {
            if (string.IsNullOrWhiteSpace(cvePlatillo))
                throw new ArgumentException("La clave del platillo no puede ser vacía.", nameof(cvePlatillo));

            using var conn = GetOpenConnection();
            string tKITS = GetTableName(conn, "KITS");     // KITS01..KITS99
            string tINVE = GetTableName(conn, "INVE");
            string tMULT = GetTableName(conn, "MULT");

            var sql = $@"
SELECT
    K.CVE_PROD,
    I.DESCR,
    I.UNI_MED,
    COALESCE(K.PORCEN, 0),
    COALESCE(K.CANTIDAD, 0),
    COALESCE(M.EXIST, I.EXIST)
FROM {tKITS} K
LEFT JOIN {tINVE} I  ON I.CVE_ART = K.CVE_PROD
LEFT JOIN {tMULT} M  ON M.CVE_ART = K.CVE_PROD
                    AND (@ALM IS NULL OR M.CVE_ALM = @ALM)
WHERE K.CVE_ART = @C
ORDER BY K.CVE_PROD";

            using var cmd = new FbCommand(sql, conn);
            cmd.Parameters.Add("@C", FbDbType.VarChar, 30).Value = cvePlatillo;
            cmd.Parameters.Add("@ALM", FbDbType.Integer).Value = (object?)almacen ?? DBNull.Value;

            var list = new List<RecetaItemDto>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new RecetaItemDto
                {
                    Clave = rd.IsDBNull(0) ? "" : rd.GetString(0).Trim(),
                    Descripcion = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                    Unidad = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim(),
                    Porcentaje = rd.IsDBNull(3) ? 0m : Convert.ToDecimal(rd.GetValue(3)),
                    Cantidad = rd.IsDBNull(4) ? 0m : Convert.ToDecimal(rd.GetValue(4)),
                    Existencia = rd.IsDBNull(5) ? 0m : Convert.ToDecimal(rd.GetValue(5))
                });
            }
            return list;
        }
    }
}
