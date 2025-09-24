using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;

namespace PROYECTO_RESIDENCIAS
{
    public static class SaeDb
    {
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
                using var cmd2 = new FbCommand("SELECT FIRST 1 CVE_ART, DESCR FROM INVE01", conn);
                using var r = cmd2.ExecuteReader();
                if (r.Read())
                {
                    // ok, hay datos
                }
            }
            catch
            {
                // si INVE01 no existe en tu demo, no lo consideres error fatal
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
//este si sirve