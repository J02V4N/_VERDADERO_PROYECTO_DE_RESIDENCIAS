using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROYECTO_RESIDENCIAS
{
    public partial class FormNuevoProducto : Form
    {
        public string CveArt => txtClave.Text;
        public string Descripcion => txtDescripcion.Text;
        public string Unidad => txtUnidad.Text;

        public FormNuevoProducto()
        {
            InitializeComponent();

            // ✅ Estos controles ya existen porque InitializeComponent() ya corrió
            lblPrefijo.Text = "Prep";

            // Si quieres mover el textbox a la derecha para dejar espacio al label:
            txtClave.Location = new Point(txtClave.Location.X + 40, txtClave.Location.Y);

            // La parte editable después de "Prep" = máx 12
            txtClave.MaxLength = 12;

            // Si conectaste el click aquí:
            btnOk.Click += BtnOk_Click;
        }


        private void BtnOk_Click(object? sender, EventArgs e)
        {
            // Parte editable (después de "Prep")
            var parte = (txtClave.Text ?? "").Trim();

            // 1) Debe haber algo después de "Prep"
            if (parte.Length == 0)
            {
                MessageBox.Show("Agrega contenido a la clave después de 'Prep'.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // 2) No más de 12 en la parte editable (ya reforzado con MaxLength)
            if (parte.Length > 12)
            {
                MessageBox.Show("La parte editable de la clave debe tener máximo 12 caracteres.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // 3) Clave final = "Prep" + parte (total máx 16)
            var clave = "Prep" + parte;
            if (clave.Length > 16)
            {
                MessageBox.Show("La clave debe tener máximo 16 caracteres (incluyendo 'Prep').",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Descripción (máx 40)
            var descr = (txtDescripcion.Text ?? string.Empty).Trim();
            if (descr.Length == 0)
            {
                MessageBox.Show("La descripción es obligatoria.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
            if (descr.Length > 40)
            {
                MessageBox.Show("La descripción debe tener máximo 40 caracteres.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Unidad por defecto
            var um = (txtUnidad.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(um)) um = "PZA";

            // Escribe los valores normalizados en los TextBox (por si los lees afuera)
            txtClave.Text = clave;          // ahora ya incluye "Prep"
            txtDescripcion.Text = descr;
            txtUnidad.Text = um;

            // Deja continuar el DialogResult=OK
        }

    }
}
