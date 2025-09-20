using FirebirdSql.Data.FirebirdClient;
using System.Collections.Generic;
using static PROYECTO_RESIDENCIAS.Form1;

namespace PROYECTO_RESIDENCIAS
{
    public static class SaeCatalog
    {
        public static List<Platillo> CargarArticulosBasicos(int empresa, string server = "127.0.0.1", int port = 3050)
        {
            var list = new List<Platillo>();
            var fdb = Sae9Locator.FindSaeDatabase(empresa);
            using var conn = SaeDb.CreateConnection(fdb, server, port, "SYSDBA", "masterkey", "ISO8859_1");
            conn.Open();

            using var cmd = new FbCommand(@"
SELECT FIRST 1000
       CVE_ART, DESCR, UNI_MED, UNI_ALT, FAC_CONV
FROM INVE01
ORDER BY CVE_ART", conn);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var clave = rd["CVE_ART"]?.ToString()?.Trim();
                var descr = rd["DESCR"]?.ToString()?.Trim();

                // Precio: por ahora 0 (o deja el que ya manejas en tu seed/UI).
                list.Add(new Platillo
                {
                    Clave = clave,
                    Nombre = descr,
                    Precio = 0m,
                    RequierePeso = false // puedes marcar pesables desde tu Aux más adelante
                });
            }
            return list;
        }
    }
}

