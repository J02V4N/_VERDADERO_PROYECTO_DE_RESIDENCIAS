using System;
using System.IO;
using System.Windows.Forms;

namespace GastroSAE
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); }
            catch { try { Application.SetHighDpiMode(HighDpiMode.SystemAware); } catch { } }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string saeFdb = ResolveAndPersistSaePath();

                // Inicializa la conexión global para que todo el sistema use la misma ruta.
                var conTmp = SaeDb.CreateConnection(
                    databasePath: saeFdb,
                    server: "127.0.0.1",
                    port: 3050,
                    user: "SYSDBA",
                    password: "masterkey",
                    charset: "ISO8859_1");
                SaeDb.Initialize(conTmp.ConnectionString);

                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo resolver una conexión válida a la base de Aspel SAE.\n\n" + ex.Message,
                    "Error de configuración",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string ResolveAndPersistSaePath()
        {
            string auxPath;
            using var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");

            // 1) Si ya existe en configuración y el archivo sigue existiendo, úsalo.
            var configured = AuxDbInitializer.GetConfig(auxConn, "SAE_FDB")?.Trim();
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                AuxDbInitializer.UpsertConfig(auxConn, "SAE_FDB", configured);
                return configured;
            }

            // 2) Intento automático sobre la Empresa 01 (ruta típica de trabajo actual).
            if (Sae9Locator.TryFindSaeDatabase(1, out var autoPath, out var locateError) && File.Exists(autoPath))
            {
                AuxDbInitializer.UpsertConfig(auxConn, "SAE_FDB", autoPath);
                return autoPath;
            }

            // 3) Error claro. Ya no mostramos la pantalla temporal de selección.
            throw new FileNotFoundException(
                "No se encontró automáticamente la BD de SAE (Empresa 01) y no hay una ruta guardada en configuración. " +
                "Configura primero la ruta SAE_FDB en la tabla CONFIG de la BD Aux o deja la base en una ruta estándar de Aspel.",
                locateError);
        }
    }
}
