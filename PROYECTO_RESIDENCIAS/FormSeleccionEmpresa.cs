using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FirebirdSql.Data.FirebirdClient;

namespace PROYECTO_RESIDENCIAS
{
    public class SaeCompanyOption
    {
        public string Display { get; set; }
        public string FdbPath { get; set; }
        public override string ToString() => Display;
    }

    public partial class FormSeleccionEmpresa : Form
    {
        private ComboBox cboBases;
        private TextBox txtUser;
        private TextBox txtPass;
        private TextBox txtServer;
        private TextBox txtPort;
        private Button btnExaminar;
        private Button btnProbar;
        private Button btnContinuar;
        private Button btnCancelar;
        private Label lblInfo;
        private Label lblUser;
        private Label lblPass;
        private Label lblServer;
        private Label lblPort;


        public string SelectedFdbPath { get; private set; }
        public string SelectedConnectionString { get; private set; }

        public FormSeleccionEmpresa()
        {
            InitializeComponent();
            CargarCandidatos();
            ValidarUI();
        }

        private void InitializeComponent()
        {
            this.Text = "Seleccionar empresa (Temporal)";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Width = 780;
            this.Height = 360;

            lblInfo = new Label
            {
                Left = 15,
                Top = 10,
                Width = 740,
                Text = "Selecciona la base de datos (.FDB) de la empresa ASPel SAE que usarás para las pruebas.\r\n" +
                       "Puedes autodetectar, elegir de la lista o examinar manualmente."
            };

            cboBases = new ComboBox { Left = 15, Top = 60, Width = 610, DropDownStyle = ComboBoxStyle.DropDownList };
            btnExaminar = new Button { Left = 635, Top = 58, Width = 110, Text = "Examinar…" };
            btnExaminar.Click += BtnExaminar_Click;

            lblUser = new Label { Left = 15, Top = 105, Width = 120, Text = "Usuario:" };
            txtUser = new TextBox { Left = 140, Top = 100, Width = 160, Text = "SYSDBA" };

            lblPass = new Label { Left = 320, Top = 105, Width = 120, Text = "Password:" };
            txtPass = new TextBox { Left = 405, Top = 100, Width = 160, UseSystemPasswordChar = true, Text = "masterkey" };

            lblServer = new Label { Left = 15, Top = 145, Width = 120, Text = "Servidor:" };
            txtServer = new TextBox { Left = 140, Top = 140, Width = 160, Text = "localhost" };

            lblPort = new Label { Left = 320, Top = 145, Width = 120, Text = "Puerto:" };
            txtPort = new TextBox { Left = 405, Top = 140, Width = 160, Text = "3050" };

            btnProbar = new Button { Left = 15, Top = 190, Width = 150, Text = "Probar conexión" };
            btnProbar.Click += BtnProbar_Click;

            btnContinuar = new Button { Left = 590, Top = 260, Width = 155, Text = "Continuar →", Enabled = false };
            btnContinuar.Click += BtnContinuar_Click;

            btnCancelar = new Button { Left = 15, Top = 260, Width = 120, Text = "Cancelar" };
            btnCancelar.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblInfo);
            this.Controls.Add(cboBases);
            this.Controls.Add(btnExaminar);
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPass);
            this.Controls.Add(lblServer);
            this.Controls.Add(txtServer);
            this.Controls.Add(lblPort);
            this.Controls.Add(txtPort);
            this.Controls.Add(btnProbar);
            this.Controls.Add(btnContinuar);
            this.Controls.Add(btnCancelar);
        }

        private void CargarCandidatos()
        {
            var patrones = new[]
            {
                @"C:\Program Files (x86)\Aspel",
                @"C:\Program Files\Aspel",
                @"C:\ProgramData\Aspel",
                @"C:\ASPEL"
            };

            var encontrados = new List<SaeCompanyOption>();

            foreach (var raiz in patrones)
            {
                if (!Directory.Exists(raiz)) continue;

                try
                {
                    // Buscar SAE*\Empresas\Empresa*\*.fdb
                    var saes = Directory.EnumerateDirectories(raiz, "SAE*", SearchOption.TopDirectoryOnly);
                    foreach (var dirSae in saes)
                    {
                        var empresasDir = Path.Combine(dirSae, "Empresas");
                        if (!Directory.Exists(empresasDir)) continue;

                        var empresas = Directory.EnumerateDirectories(empresasDir, "Empresa*", SearchOption.TopDirectoryOnly);
                        foreach (var dirEmp in empresas)
                        {
                            // Busca archivos .FDB dentro de la empresa (casi siempre en la misma carpeta o subcarpetas)
                            IEnumerable<string> fdbs;
                            try
                            {
                                fdbs = Directory.EnumerateFiles(dirEmp, "*.fdb", SearchOption.AllDirectories);
                            }
                            catch
                            {
                                continue;
                            }

                            foreach (var fdb in fdbs)
                            {
                                var display = $"{Path.GetFileName(dirEmp)} – {Path.GetFileName(fdb)}";
                                encontrados.Add(new SaeCompanyOption
                                {
                                    Display = display,
                                    FdbPath = fdb
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Ignorar errores de permisos/rutas.
                }
            }

            // Ordenar por nombre de empresa y archivo
            foreach (var item in encontrados.OrderBy(x => x.Display))
                cboBases.Items.Add(item);

            if (cboBases.Items.Count > 0) cboBases.SelectedIndex = 0;
        }

        private void BtnExaminar_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Seleccionar base de datos Firebird (.FDB)";
                dlg.Filter = "Firebird DB (*.fdb)|*.fdb|Todos los archivos|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var display = $"{Path.GetFileNameWithoutExtension(dlg.FileName)} – {Path.GetFileName(dlg.FileName)}";
                    var opt = new SaeCompanyOption { Display = display, FdbPath = dlg.FileName };
                    cboBases.Items.Add(opt);
                    cboBases.SelectedItem = opt;
                }
            }
        }

        private string BuildConnectionString(string fdbPath)
        {
            // NOTA: Ajusta Charset si lo necesitas (ej. WIN1252) según tus datos Aspel
            var csb = new FbConnectionStringBuilder
            {
                Database = fdbPath,
                DataSource = string.IsNullOrWhiteSpace(txtServer.Text) ? "localhost" : txtServer.Text.Trim(),
                Port = int.TryParse(txtPort.Text, out var p) ? p : 3050,
                UserID = string.IsNullOrWhiteSpace(txtUser.Text) ? "SYSDBA" : txtUser.Text.Trim(),
                Password = txtPass.Text ?? string.Empty,
                Dialect = 3,
                Charset = "NONE",
                Pooling = true,
                // ServerType = FbServerType.Default  // Deja en Default; si usas embedded, puedes cambiarlo.
            };
            return csb.ToString();
        }

        private void BtnProbar_Click(object sender, EventArgs e)
        {
            var opt = cboBases.SelectedItem as SaeCompanyOption;
            if (opt == null)
            {
                MessageBox.Show(this, "Selecciona o examina una base .FDB.", "Falta selección",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Construye CS a partir de la ruta elegida
            var cs = BuildConnectionString(opt.FdbPath);

            try
            {
                using (var con = new FirebirdSql.Data.FirebirdClient.FbConnection(cs))
                {
                    con.Open();
                    using (var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand("SELECT 1 FROM RDB$DATABASE", con))
                    {
                        var result = cmd.ExecuteScalar();
                        if (Convert.ToInt32(result) != 1)
                            throw new Exception("La consulta de prueba no devolvió el valor esperado.");
                    }
                }

                // Si llegamos aquí, la conexión fue OK:
                SelectedConnectionString = cs;
                SelectedFdbPath = opt.FdbPath;

                // Guardamos la ruta elegida en la BD auxiliar (CONFIG.SAE_FDB)
                try
                {
                    string auxPath;
                    using (var auxConn = AuxDbInitializer.EnsureCreated(out auxPath, charset: "UTF8"))
                    {
                        AuxDbInitializer.UpsertConfig(auxConn, "SAE_FDB", SelectedFdbPath);
                    }
                }
                catch (Exception exAux)
                {
                    MessageBox.Show(this,
                        "Conexión OK, pero no pude guardar SAE_FDB en la BD auxiliar:\r\n\r\n" + exAux.Message,
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // No bloqueamos la continuidad: ya hay conexión OK
                }

                MessageBox.Show(this, "Conexión OK. Puedes continuar.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnContinuar.Enabled = true;
            }
            catch (Exception ex)
            {
                btnContinuar.Enabled = false;
                MessageBox.Show(this,
                    "No fue posible conectar:\r\n\r\n" + ex.Message,
                    "Error de conexión",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnContinuar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedConnectionString))
            {
                MessageBox.Show(this, "Primero realiza una prueba de conexión exitosa.", "Atención",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        private void ValidarUI()
        {
            btnContinuar.Enabled = false;
        }
    }
}



//este si funciona