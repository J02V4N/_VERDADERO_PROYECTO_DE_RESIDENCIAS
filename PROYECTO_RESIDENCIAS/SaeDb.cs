using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace PROYECTO_RESIDENCIAS
{

    public static class SaeDb
    {
        public static void ResetSaeSuffixCache() => _cachedSaeSuffix = null;


        public static string ConnectionString { get; private set; }

        /// <summary>
        /// Inicializa la cadena de conexión para todo el app.
        /// Llama esto una sola vez al inicio (desde FormSeleccionEmpresa).
        /// </summary>
        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("ConnectionString no puede ser vacío.");

            ConnectionString = connectionString;
        }

        /// <summary>
        /// Obtiene una conexión nueva (caller la cierra).
        /// </summary>
        public static FbConnection GetConnection()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("SaeDb no ha sido inicializado. Llama Initialize() primero.");

            return new FbConnection(ConnectionString);
        }

        /// <summary>
        /// Prueba rápida; úsala si necesitas validar en otra parte.
        /// </summary>
        


        public static FbConnection CreateConnection(
            string databasePath,
            string server = "127.0.0.1",
            int port = 3050,
            string user = "SYSDBA",
            string password = "masterkey",
            string charset = "ISO8859_1")
        {
            var cs = new FbConnectionStringBuilder
            {
                DataSource = server,
                Port = port,
                Database = databasePath,
                UserID = user,
                Password = password,
                Charset = charset,
                Dialect = 3,
                Pooling = true,
                // Si usas cliente embebido x86 local, descomenta:
                ClientLibrary = "fbclient.dll"
            };

            return new FbConnection(cs.ToString());
        }

        
        //----------------------------------------------------------------------------------------------el original
        public static void TestConnection(FbConnection conn)
        {
            conn.Open();
            using var cmd = new FbCommand("SELECT 1 FROM RDB$DATABASE", conn);
            var o = cmd.ExecuteScalar();
            if (Convert.ToInt32(o) != 1)
                throw new Exception("Prueba SELECT 1 falló.");

            // Prueba mínima contra una tabla típica de SAE 9 (ajústala si tu esquema usa otro nombre)
            try
            {
                var invTable = GetTableName(conn, "INVE");              // ← INVE##
                using var cmd2 = new FirebirdSql.Data.FirebirdClient.FbCommand(
                    $"SELECT FIRST 1 CVE_ART, DESCR FROM {invTable}", conn);
                using var r = cmd2.ExecuteReader();
                // ok si no truena
            }
            catch
            {
                // si no existe INVE##, no lo consideres fatal (ya probamos SELECT 1 antes)
            }
            finally
            {
                conn.Close();
            }

        }
        //---------------------------------------------------------------------------------------------- el original

        private static string _cachedSaeSuffix;

        public static string GetCompanySuffix(FbConnection con)
        {
            if (!string.IsNullOrEmpty(_cachedSaeSuffix))
                return _cachedSaeSuffix;

            // Busca nombres tipo INVE##
            using var cmd = new FbCommand(
                "SELECT TRIM(RDB$RELATION_NAME) " +
                "FROM RDB$RELATIONS " +
                "WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL " +
                "AND RDB$RELATION_NAME STARTING WITH 'INVE'", con);

            using var rd = cmd.ExecuteReader();
            var posibles = new List<string>();
            while (rd.Read())
            {
                var name = rd.GetString(0).Trim();   // p.ej. INVE07
                var m = Regex.Match(name, @"^INVE(\d{2})$");
                if (m.Success) posibles.Add(m.Groups[1].Value);
            }

            if (posibles.Count == 0)
                throw new Exception("No encontré ninguna tabla INVE## en esta BD. ¿Seguro es una BD de Aspel SAE?");

            // Normalmente habrá solo una; tomamos la primera
            _cachedSaeSuffix = posibles[0];
            return _cachedSaeSuffix;
        }

        public static string GetTableName(FbConnection con, string baseName /* ej. INVE, CLIE, PROV, MINVE, FACTF */)
        {
            var sfx = GetCompanySuffix(con); // ej. "07"
            var candidato = baseName + sfx;  // ej. "INVE07"

            using (var cmd = new FbCommand(
                "SELECT COUNT(*) FROM RDB$RELATIONS " +
                "WHERE RDB$SYSTEM_FLAG=0 AND TRIM(RDB$RELATION_NAME)=@t", con))
            {
                cmd.Parameters.Add("@t", FbDbType.VarChar).Value = candidato;
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 1) return candidato;
            }

            // Fallback: cualquier tabla que empiece por baseName y termine en 2 dígitos
            using (var cmd2 = new FbCommand(
                "SELECT TRIM(RDB$RELATION_NAME) " +
                "FROM RDB$RELATIONS " +
                "WHERE RDB$SYSTEM_FLAG=0 AND RDB$RELATION_NAME STARTING WITH @pref", con))
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



        //aqui empiezan los nuevos cambios, asi que, atencion, por si las dudas, todo lo que esta depues de esto, es a parter de las 10:10 am del 10 de octubre (mes 10 xD)

        public class PlatilloDto
        {
            public string Clave { get; set; }
            public string Descripcion { get; set; }
            public string Unidad { get; set; }
            public decimal Precio { get; set; }
            public decimal Existencia { get; set; }
            public string Status { get; set; }
        }

        public static List<PlatilloDto> ListarPlatillos(
    int listaPrecio = 1,
    int? almacen = 1,
    string clavePrefix = "Prep",   // por defecto, solo CVE_ART que comiencen con PREP
    string linProd = "Prep"          // opcional, por si luego filtras por línea
)
        {
            using var conn = GetOpenConnection();

            string tINVE = GetTableName(conn, "INVE");
            string tPXP = GetTableName(conn, "PRECIO_X_PROD");
            string tMULT = GetTableName(conn, "MULT");

            
            // WHERE dinámico (ahora case-insensitive)
            var where = "WHERE I.STATUS = 'A' ";
            if (!string.IsNullOrWhiteSpace(clavePrefix))
                where += "AND UPPER(I.CVE_ART) STARTING WITH @PFX ";  // <- clave
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

            using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(sql, conn);
            cmd.Parameters.Add("@LISTA", FbDbType.Integer).Value = listaPrecio;
            cmd.Parameters.Add("@ALM", FbDbType.Integer).Value = (object?)almacen ?? DBNull.Value;
            if (!string.IsNullOrWhiteSpace(clavePrefix))
                cmd.Parameters.Add("@PFX", FbDbType.VarChar, 32).Value = clavePrefix.ToUpperInvariant(); // <- en mayúsculas
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

            return list;  // ← clave: siempre devolvemos la lista
        }



        /// <summary>
        /// Resuelve nombres reales con sufijo (p.ej. "INVE" -> "INVE01") consultando RDB$RELATIONS.
        /// Si ya tienes un helper equivalente, usa ese y borra este.
        /// </summary>
        private static string ResolveTableName(FbConnection conn, string baseName)
        {
            // Busca la primera coincidencia exacta con sufijo 2 dígitos (01..99) o sin sufijo.
            using var cmd = new FbCommand(@"
        SELECT TRIM(RDB$RELATION_NAME)
        FROM RDB$RELATIONS
        WHERE RDB$SYSTEM_FLAG = 0
          AND (RDB$RELATION_NAME = @BN
               OR RDB$RELATION_NAME STARTING WITH @BN)
        ORDER BY RDB$RELATION_NAME", conn);

            cmd.Parameters.Add("@BN", FbDbType.VarChar, 31).Value = baseName;
            using var rd = cmd.ExecuteReader();
            string? found = null;
            while (rd.Read())
            {
                var name = rd.GetString(0).Trim();
                // Preferimos el patrón con sufijo de 2 dígitos si existe (INVE01, PRECIO_X_PROD01, MULT01, ...)
                if (name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                    found ??= name;
                if (name.Length == baseName.Length + 2 &&
                    int.TryParse(name.Substring(baseName.Length, 2), out _))
                {
                    found = name;
                    break;
                }
            }
            if (found == null)
                throw new InvalidOperationException($"No se encontró la tabla para '{baseName}??' en la base de SAE.");
            return found;
        }

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
                    throw new InvalidOperationException("CONFIG.SAE_FDB está vacío. Selecciona la empresa primero.");

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
        public static string ObtenerDescripcionArticulo(string clave)
        {
            using var con = GetOpenConnection();
            string tINVE = GetTableName(con, "INVE");
            using var cmd = new FbCommand($"SELECT DESCR FROM {tINVE} WHERE CVE_ART=@C", con);
            cmd.Parameters.Add("@C", FbDbType.VarChar, 30).Value = clave;
            var o = cmd.ExecuteScalar();
            return o?.ToString()?.Trim() ?? clave;
        }

    }
}
