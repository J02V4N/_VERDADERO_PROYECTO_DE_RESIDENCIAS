//using FirebirdSql.Data.FirebirdClient;
//using System;
//using System.Windows.Forms;

//namespace PROYECTO_RESIDENCIAS
//{
//    internal static class Program
//    {
//        [STAThread]
//        static void Main()
//        {
//            using (var sel = new FormSeleccionEmpresa())
//            {
//                var dr = sel.ShowDialog();
//                if (dr != DialogResult.OK || string.IsNullOrWhiteSpace(sel.SelectedFdbPath))
//                    return;

//                var cs = new FbConnectionStringBuilder
//                {
//                    Database = sel.SelectedFdbPath,
//                    DataSource = "127.0.0.1",
//                    Port = 3050,
//                    UserID = "SYSDBA",
//                    Password = "masterkey",
//                    Charset = "ISO8859_1",
//                    Dialect = 3,
//                    Pooling = true
//                }.ToString();

//                SaeDb.Initialize(cs);
//            }

//            using (var ft = new FormSeleccionTurno())
//            {
//                var dr = ft.ShowDialog();
//                if (dr != DialogResult.OK) return; // no continua si no eligen/abren turno
//                                                   // Si quieres guardar en memoria quién está:
//                                                   // AppSession.SetTurno(ft.IdTurnoSeleccionado, responsableX);
//            }
//            Application.Run(new Form1());
//        }
//    }
//}

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
