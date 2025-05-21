using ComputerInfo.Properties;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ComputerInfo
{
    class InfoForm : Form
    {
        // Declare UI components
        private TabControl tabControl;
        private TabPage infoTab;
        private TabPage settingsTab;
        private CheckBox chkAutoStart;
        private TextBox txtboxNom;
        private TextBox txtboxCognoms;
        private ComboBox comboTipusServeis;
        private Label lblDbStatus;

        // Constructor to initialize the form and its components
        public InfoForm(string computerName, string osInfo)
        {
            this.Text = "Informació de l'ordinador";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(500, 200);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            infoTab = new TabPage("Informació");
            var label = new Label
            {
                AutoSize = true,
                Location = new System.Drawing.Point(10, 10),
                Text = $"Nom de l'ordinador: {computerName}\n" +
                       $"Sistema Operatiu: {osInfo}"
            };
            infoTab.Controls.Add(label);

            settingsTab = new TabPage("Configuració");
            chkAutoStart = new CheckBox
            {
                Text = "Iniciar automàticament al iniciar Windows",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 20)
            };
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.CheckedChanged += ChkAutoStart_CheckedChanged;
            settingsTab.Controls.Add(chkAutoStart);

            var labelNom = new Label
            {
                Text = "Nom:",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 50)
            };
            txtboxNom = new TextBox
            {
                Location = new System.Drawing.Point(100, 50),
                Width = 200
            };
            txtboxNom.Text = Properties.Settings.Default.Nom;
            settingsTab.Controls.Add(labelNom);
            settingsTab.Controls.Add(txtboxNom);

            var labelCognoms = new Label
            {
                Text = "Cognoms:",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 80)
            };
            txtboxCognoms = new TextBox
            {
                Location = new System.Drawing.Point(100, 80),
                Width = 200
            };
            txtboxCognoms.Text = Properties.Settings.Default.Cognoms;
            settingsTab.Controls.Add(labelCognoms);
            settingsTab.Controls.Add(txtboxCognoms);

            var labelTipusServeis = new Label
            {
                Text = "Tipus de servei:",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 110)
            };
            settingsTab.Controls.Add(labelTipusServeis);

            comboTipusServeis = new ComboBox
            {
                Location = new System.Drawing.Point(100, 110),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            settingsTab.Controls.Add(comboTipusServeis);

            lblDbStatus = new Label
            {
                Text = "Comprovant connexió a la base de dades...",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 150),
                ForeColor = Color.Black
            };
            settingsTab.Controls.Add(lblDbStatus);

            tabControl.TabPages.Add(infoTab);
            tabControl.TabPages.Add(settingsTab);

            this.Controls.Add(tabControl);

            ComprovaConnexioBD();
            CarregaTipusServeis();
            MostraInfoUsuariIServei(computerName, osInfo);
        }

        // event handler for the checkbox to set auto start
        private void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoStart(chkAutoStart.Checked);
        }

        // checks if the application is set to start automatically with Windows
        private bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue(Application.ProductName) != null;
            }
        }

        // sets the application to start automatically with Windows
        private void SetAutoStart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    key.SetValue(Application.ProductName, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(Application.ProductName, false);
                }
            }
        }

        // checks the connection to the database and updates the label accordingly
        private void ComprovaConnexioBD()
        {
            try
            {
                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    lblDbStatus.Text = "Connectat a la base de dades!";
                    lblDbStatus.ForeColor = Color.Green;
                }
            }
            catch (Exception ex)
            {
                lblDbStatus.Text = "Error de connexió a la base de dades: " + ex.Message;
                lblDbStatus.ForeColor = Color.Red;
            }
        }

        // loads the type of services from the database and updates the combo box
        private void CarregaTipusServeis()
        {
            try
            {
                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT distinct nom FROM serveis ORDER by nom";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var serveis = new System.Collections.Generic.List<string>();

                        serveis.Add("No definit");

                        while (reader.Read())
                        {
                            serveis.Add(reader.GetString(0));
                        }

                        comboTipusServeis.Items.Clear();
                        comboTipusServeis.Items.AddRange(serveis.ToArray());

                        if (!string.IsNullOrEmpty(Properties.Settings.Default.TipusServei) &&
                            comboTipusServeis.Items.Contains(Properties.Settings.Default.TipusServei))
                        {
                            comboTipusServeis.SelectedItem = Properties.Settings.Default.TipusServei;
                        }
                        else
                        {
                            comboTipusServeis.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                comboTipusServeis.Items.Clear();
                comboTipusServeis.Items.AddRange(new string[] { "No definit" });
                comboTipusServeis.SelectedIndex = 0;
                MessageBox.Show("Error carregant tipus de serveis: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // retrieves the user and service information from the database and displays it in the info tab
        private void MostraInfoUsuariIServei(string computerName, string osInfo)
        {
            try
            {
                string codiAJT = new string(computerName.Where(char.IsDigit).ToArray());

                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"SELECT usuaris.nomcognoms, serveis.nom
                                   FROM elements
                                   INNER JOIN usuaris ON usuaris.idusuari = elements.usuari
                                   INNER JOIN serveis ON serveis.idservei = usuaris.servei_principal
                                   WHERE elements.codiAJT = @codiAJT
                                   LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiAJT", codiAJT);
                        using (var reader = cmd.ExecuteReader())
                        {
                            string text;
                            if (reader.Read())
                            {
                                string nomCognoms = reader.GetString(0);
                                string servei = reader.GetString(1);
                                text = $"Nom i cognoms: {nomCognoms}\nServei principal: {servei}";
                            }
                            else
                            {
                                text = "No s'ha trobat informació a la base de dades.";
                            }
                            var labelInfo = new Label
                            {
                                AutoSize = true,
                                Location = new System.Drawing.Point(10, 40),
                                Text = text
                            };
                            infoTab.Controls.Add(labelInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var labelInfo = new Label
                {
                    AutoSize = true,
                    Location = new System.Drawing.Point(10, 40),
                    Text = "Error: " + ex.Message
                };
                infoTab.Controls.Add(labelInfo);
            }
        }

        // saves the settings when the form is closing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Properties.Settings.Default.Nom = txtboxNom.Text;
            Properties.Settings.Default.Cognoms = txtboxCognoms.Text;
            Properties.Settings.Default.TipusServei = comboTipusServeis.SelectedItem?.ToString();
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }
    }
}
