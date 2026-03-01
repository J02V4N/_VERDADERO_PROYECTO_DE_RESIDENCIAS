using System;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string saeFdb = null;

            try
            {
                string auxPath;
                using var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
                saeFdb = AuxDbInitializer.GetConfig(auxConn, "SAE_FDB"); // si ya tienes este método
            }
            catch
            {
                // si falla la lectura de config, saeFdb quedará null
            }

            if (string.IsNullOrWhiteSpace(saeFdb) || !File.Exists(saeFdb))
            {
#if DEBUG
                using var sel = new FormSeleccionEmpresa();
                if (sel.ShowDialog() != DialogResult.OK)
                    return; // usuario canceló

                saeFdb = sel.SelectedFdbPath;
#else
        MessageBox.Show("No se encontró una ruta válida a la base de Aspel SAE. Contacta al administrador.",
                        "Error de configuración",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
        return;
#endif
            }

            // A partir de aquí, ya tienes saeFdb válido
            // → sigues con selección de turno, Form1, etc.
            Application.Run(new Form1(/* si quieres, pásale saeFdb por ctor */));
        }

    }
}
