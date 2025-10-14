using System;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1) Elegir empresa (temporal de pruebas)
            using (var f = new FormSeleccionEmpresa())
            {
                if (f.ShowDialog() != DialogResult.OK) return;

                // inicializa SaeDb con la cadena probada
                SaeDb.Initialize(f.SelectedConnectionString);
            }

            // 2) Selección/creación de turno
            using (var t = new FormSeleccionTurno())
            {
                if (t.ShowDialog() != DialogResult.OK) return;
                // Si necesitas pasar el IdTurno a Form1, agrega un constructor/prop
            }

            // 3) Arranca principal
            Application.Run(new Form1());
        }
    }
}
