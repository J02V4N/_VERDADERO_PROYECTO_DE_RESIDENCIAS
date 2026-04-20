using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GastroSAE
{
    internal static class AppIcon
    {
        private static Icon? _cached;

        public static Icon? Get()
        {
            if (_cached != null) return _cached;

            try
            {
                var iconPath = Path.Combine(Application.StartupPath, "GastroSAE.ico");
                if (File.Exists(iconPath))
                {
                    _cached = new Icon(iconPath);
                    return _cached;
                }
            }
            catch { }

            try
            {
                _cached = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return _cached;
            }
            catch
            {
                return null;
            }
        }

        public static void Apply(Form form)
        {
            if (form == null) return;

            try
            {
                var icon = Get();
                if (icon != null)
                {
                    form.Icon = icon;
                    form.ShowIcon = true;
                }
            }
            catch { }
        }
    }
}
