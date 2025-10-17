/*using System;
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

            // 2) Selecci�n/creaci�n de turno
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
*/

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

            // 1) Seleccionar empresa y probar conexi�n (guarda SAE_FDB en AUX.CONFIG)
            using (var f = new FormSeleccionEmpresa())
            {
                if (f.ShowDialog() != DialogResult.OK) return;
                // Si necesitas un Initialize extra, puedes ponerlo aqu�.
            }

            // 2) Seleccionar/abrir turno
            using (var t = new FormSeleccionTurno())
            {
                if (t.ShowDialog() != DialogResult.OK) return;
                // Si ocupas pasar IdTurno a Form1, agr�gale una propiedad p�blica y as�gnala aqu�.
            }

            // 3) Abrir principal
            Application.Run(new Form1());
        }
    }
}
