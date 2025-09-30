using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace PROYECTO_RESIDENCIAS
{
    //lo elimine para ver si ya no hay problemas

    public static class SaeDb
    {

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
                // ClientLibrary = "fbclient.dll"
            };

            return new FbConnection(cs.ToString());
        }

        //---------------------------------------------------------------------------------------------- el nuevo
        //public static bool TestConnection(out string error)
        //{
           // error = null;
           // try
            //{
               // using (var con = GetConnection())
               // {
                 //   con.Open();
                  //  using (var cmd = new FbCommand("SELECT 1 FROM RDB$DATABASE", con))
                  //  {
                    //    var r = cmd.ExecuteScalar();
                    //    return Convert.ToInt32(r) == 1;
                   // }
               // }
          //  }
           // catch (Exception ex)
           // {
            //    error = ex.Message;
             //   return false;
            //}
       // }
        //-------------------------------------------------------------------------------------------- el nuevo


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

    }
}
