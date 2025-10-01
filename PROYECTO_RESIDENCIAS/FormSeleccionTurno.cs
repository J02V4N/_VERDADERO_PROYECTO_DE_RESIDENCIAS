using System;
using System.Windows.Forms;

namespace PROYECTO_RESIDENCIAS
{
    public class FormSeleccionTurno : Form
    {
        private Label lblInfo;
        private TextBox txtResponsable, txtObs;
        private Button btnUsar, btnAbrir, btnSalir;

        public int IdTurnoSeleccionado { get; private set; }

        public FormSeleccionTurno()
        {
            Text = "Seleccionar/Abrir turno";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 480; Height = 230;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            lblInfo = new Label { Left = 15, Top = 15, Width = 440, Height = 40, Text = "Buscando turno abierto..." };

            var lblR = new Label { Left = 15, Top = 65, Width = 90, Text = "Responsable:" };
            txtResponsable = new TextBox { Left = 110, Top = 60, Width = 340 };

            var lblO = new Label { Left = 15, Top = 95, Width = 90, Text = "Observación:" };
            txtObs = new TextBox { Left = 110, Top = 90, Width = 340 };

            btnUsar = new Button { Left = 110, Top = 130, Width = 110, Text = "Usar turno abierto" };
            btnAbrir = new Button { Left = 230, Top = 130, Width = 100, Text = "Abrir turno" };
            btnSalir = new Button { Left = 340, Top = 130, Width = 110, Text = "Salir" };

            btnUsar.Click += (s, e) => UsarTurno();
            btnAbrir.Click += (s, e) => AbrirTurno();
            btnSalir.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lblInfo, lblR, txtResponsable, lblO, txtObs, btnUsar, btnAbrir, btnSalir });

            Load += FormSeleccionTurno_Load;
        }

        private void FormSeleccionTurno_Load(object? sender, EventArgs e)
        {
            var info = AuxRepo.ObtenerTurnoAbiertoInfo();
            if (info != null)
            {
                lblInfo.Text = $"Turno abierto hoy: #{info.Id}  {info.Fecha:d}  " +
                               $"Inicio {info.HoraIni:hh\\:mm}  Resp: {info.Responsable}";
                btnUsar.Enabled = true;
                // prellenamos responsable por si abres otro turno
                txtResponsable.Text = info.Responsable;
            }
            else
            {
                lblInfo.Text = "No hay turno abierto hoy.";
                btnUsar.Enabled = false;
            }
        }

        private void UsarTurno()
        {
            var id = AuxRepo.GetTurnoAbiertoId();
            if (id == null) { MessageBox.Show("Ya no hay turno abierto."); return; }
            IdTurnoSeleccionado = id.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void AbrirTurno()
        {
            try
            {
                var id = AuxRepo.AbrirTurno(txtResponsable.Text, txtObs.Text);
                IdTurnoSeleccionado = id;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "No se pudo abrir el turno", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
