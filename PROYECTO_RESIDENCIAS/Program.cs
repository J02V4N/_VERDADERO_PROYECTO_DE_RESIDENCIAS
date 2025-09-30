
using System;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var sel = new FormSeleccionEmpresa())
            {
                var dr = sel.ShowDialog();
                if (dr != DialogResult.OK || string.IsNullOrWhiteSpace(sel.SelectedConnectionString))
                {
                    // Salir si no hay conexión
                    return;
                }

                // Inicializa la capa de datos con el CS elegido
                SaeDb.Initialize("User=SYSDBA;Password=masterkey;Database=C:\\RUTA\\A\\TUEMPRESA.FDB;DataSource=localhost;Port=3050;Dialect=3;Charset=NONE;Pooling=true;");

            }

            Application.Run(new Form1());
        }
    }
}
