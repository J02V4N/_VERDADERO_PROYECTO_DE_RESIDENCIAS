using System;
using System.Drawing;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    public partial class FormNuevoProducto : Form
    {
        private const string PrefijoClave = "Prep";

        // Clave final: prefijo fijo + parte editable del textbox
        public string CveArt
            => (PrefijoClave + (txtClave.Text ?? string.Empty).Trim());

        public string Descripcion
            => (txtDescripcion.Text ?? string.Empty).Trim();

        public string Unidad
            => (txtUnidad.Text ?? string.Empty).Trim();

        public string ClaveCompleta => (lblPrefijo.Text ?? "") + (txtClave.Text ?? "");
      

        public FormNuevoProducto()
        {
            InitializeComponent();

            UiStyle.Apply(this);
            UiFields.Apply(this);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            UiHints.Attach(this, new (Control control, string hint)[]
            {
                (btnOk, "Enter"),
                (btnCancel, "Esc"),
            });

            // Prefijo visual fijo
            lblPrefijo.Text = PrefijoClave;

            // Parte editable (sufijo) máx 12 caracteres
            txtClave.MaxLength = 12;

            // Descripción máx 40 caracteres (además de la validación)
            txtDescripcion.MaxLength = 40;

            // Unidad fija: "pz" (no editable)
            txtUnidad.Text = "pz";
            txtUnidad.ReadOnly = true;
            txtUnidad.TabStop = false;

            // Evitar suscripciones duplicadas si el diseñador ya lo había conectado
            btnOk.Click += BtnOk_Click;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Parte editable después del prefijo
            var parte = (txtClave.Text ?? string.Empty).Trim();

            // 1) Debe haber algo después del prefijo
            if (parte.Length == 0)
            {
                MessageBox.Show(
                    "Agrega contenido a la clave después del prefijo.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
                txtClave.Focus();
                return;
            }

            // 2) Máx 12 en la parte editable (reforzado con MaxLength)
            if (parte.Length > 12)
            {
                MessageBox.Show(
                    "La parte editable de la clave debe tener máximo 12 caracteres.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
                txtClave.Focus();
                return;
            }

            // 3) Validar que la clave completa no exceda 16 caracteres
            var claveCompleta = PrefijoClave + parte;
            if (claveCompleta.Length > 16)
            {
                MessageBox.Show(
                    "La clave debe tener máximo 16 caracteres (incluyendo el prefijo).",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
                txtClave.Focus();
                return;
            }

            // Descripción (máx 40)
            var descr = (txtDescripcion.Text ?? string.Empty).Trim();
            if (descr.Length == 0)
            {
                MessageBox.Show(
                    "La descripción es obligatoria.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
                txtDescripcion.Focus();
                return;
            }

            if (descr.Length > 40)
            {
                MessageBox.Show(
                    "La descripción debe tener máximo 40 caracteres.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
                txtDescripcion.Focus();
                return;
            }

            // Unidad fija
            var um = "pz";

            // Normalizar valores en los controles para quien los lea después
            txtClave.Text = parte;                 // solo la parte editable
            txtDescripcion.Text = descr;
            txtUnidad.Text = um;

            // No se toca DialogResult aquí: si el botón tiene DialogResult=OK
            // en el diseñador, la ventana se cerrará; si no, deberás asignarlo afuera.
        }
    }
}
