using ComputerInfo.Properties;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net;

namespace ComputerInfo
{
    class InfoForm : Form
    {
        // Declare UI components
        private TabControl tabControl;
        private TabPage infoTab;
        private TabPage settingsTab;
        private CheckBox chkAutoStart;
        private ComboBox comboUsuari;
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

            var labelIp = new Label
            {
                AutoSize = true,
                Location = new System.Drawing.Point(10, 80),
                Text = $"IP local: {ObtenerIpLocal()}"
            };
            infoTab.Controls.Add(labelIp);

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

            var labelUsuari = new Label
            {
                Text = "Usuari:",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 50)
            };
            comboUsuari = new ComboBox
            {
                Location = new System.Drawing.Point(100, 50),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboUsuari.SelectedIndexChanged += ComboUsuari_SelectedIndexChanged;
            settingsTab.Controls.Add(labelUsuari);
            settingsTab.Controls.Add(comboUsuari);

            var labelTipusServeis = new Label
            {
                Text = "Tipus de servei:",
                AutoSize = true,
                Location = new System.Drawing.Point(10, 80)
            };
            settingsTab.Controls.Add(labelTipusServeis);

            comboTipusServeis = new ComboBox
            {
                Location = new System.Drawing.Point(100, 80),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            settingsTab.Controls.Add(comboTipusServeis);

            var updateButton = new Button
            {
                Text = "Actualitzar",
                Location = new System.Drawing.Point(10, 110),
                Width = 80,
            };
            updateButton.Click += UpdateButton_Click;
            settingsTab.Controls.Add(updateButton);

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

            // 1. Remove the event handler before loading data
            comboUsuari.SelectedIndexChanged -= ComboUsuari_SelectedIndexChanged;

            CarregaUsuaris();
            CarregaTipusServeis();

            // 2. Select the user after loading both ComboBox
            string codiAJT = new string(computerName.Where(char.IsDigit).ToArray());
            string nomcognoms = ObtenerNomcognomsPorCodiAJT(codiAJT);
            if (!string.IsNullOrEmpty(nomcognoms) && comboUsuari.Items.Contains(nomcognoms))
            {
                comboUsuari.SelectedItem = nomcognoms;
            }
            else if (comboUsuari.Items.Count > 0)
            {
                comboUsuari.SelectedIndex = 0;
            }

            // 3. Re-enable the event handler and call it manually to update the service
            comboUsuari.SelectedIndexChanged += ComboUsuari_SelectedIndexChanged;
            ComboUsuari_SelectedIndexChanged(comboUsuari, EventArgs.Empty);

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

        // retrieves the computer local IP address
        private string ObtenerIpLocal()
        {
            string ipLocal = "No disponible";
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ipLocal = ip.ToString();
                        break;
                    }
                }
            }
            catch
            {
                ipLocal = "Error obtenint IP";
            }
            return ipLocal;
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

        // loads the names from the database and updates the combo box
        private void CarregaUsuaris()
        {
            try
            {
                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = "SELECT distinct nomcognoms FROM usuaris ORDER by nomcognoms";
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var usuaris = new System.Collections.Generic.List<string>();
                        usuaris.Add("No definit");
                        while (reader.Read())
                        {
                            usuaris.Add(reader.GetString(0));
                        }
                        comboUsuari.Items.Clear();
                        comboUsuari.Items.AddRange(usuaris.ToArray());
                        if (comboUsuari.Items.Count == 0)
                            return;
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.Nom) &&
                            comboUsuari.Items.Contains(Properties.Settings.Default.Nom))
                        {
                            comboUsuari.SelectedItem = Properties.Settings.Default.Nom;
                        }
                        else if (comboUsuari.Items.Count > 0)
                        {
                            comboUsuari.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                comboUsuari.Items.Clear();
                comboUsuari.Items.AddRange(new string[] { "No definit" });
                if (comboUsuari.Items.Count > 0)
                {
                    comboUsuari.SelectedIndex = 0;
                }
                MessageBox.Show("Error carregant usuaris: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // event handler for the combo box to handle user selection changes
        private void ComboUsuari_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboUsuari.Items.Count == 0)
                return;

            string usuariSeleccionat = comboUsuari.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(usuariSeleccionat) && usuariSeleccionat != "No definit")
            {
                string tipusServei = ObtenerTipusServeiDeUsuari(usuariSeleccionat);
                if (!string.IsNullOrEmpty(tipusServei) && comboTipusServeis.Items.Contains(tipusServei))
                {
                    comboTipusServeis.SelectedItem = tipusServei;
                }
                else if (comboTipusServeis.Items.Count > 0)
                {
                    comboTipusServeis.SelectedIndex = 0;
                }
            }
            else if (comboTipusServeis.Items.Count > 0)
            {
                comboTipusServeis.SelectedIndex = 0;
            }
        }

        // retrieves the service type for the selected user from the database
        private string ObtenerTipusServeiDeUsuari(string nomcognoms)
        {
            try
            {
                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    // Primero obtenemos el idusuari a partir del nombre completo
                    string sqlId = "SELECT idusuari FROM usuaris WHERE nomcognoms = @nomcognoms LIMIT 1";
                    int? idusuari = null;
                    using (var cmdId = new MySqlCommand(sqlId, conn))
                    {
                        cmdId.Parameters.AddWithValue("@nomcognoms", nomcognoms);
                        var result = cmdId.ExecuteScalar();
                        if (result != null && int.TryParse(result.ToString(), out int id))
                            idusuari = id;
                    }
                    if (idusuari == null)
                        return null;

                    // Ahora buscamos el servicio principal usando la tabla elements
                    string sql = @"SELECT serveis.nom
                           FROM elements
                           INNER JOIN usuaris ON usuaris.idusuari = elements.usuari
                           INNER JOIN serveis ON serveis.idservei = usuaris.servei_principal
                           WHERE elements.usuari = @idusuari
                           LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idusuari", idusuari.Value);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch
            {
                return null;
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

        // retrieves the full name of the user based on the codiAJT from the database
        private string ObtenerNomcognomsPorCodiAJT(string codiAJT)
        {
            try
            {
                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"SELECT usuaris.nomcognoms
                                   FROM elements
                                   INNER JOIN usuaris ON usuaris.idusuari = elements.usuari
                                   WHERE elements.codiAJT = @codiAJT
                                   LIMIT 1";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@codiAJT", codiAJT);
                        var result = cmd.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // saves the settings when the form is closing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Properties.Settings.Default.Nom = comboUsuari.SelectedItem?.ToString();
            Properties.Settings.Default.TipusServei = comboTipusServeis.SelectedItem?.ToString();
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }

        // event handler for the update button click
        private void UpdateButton_Click(object sender, EventArgs e)
        {
            string usuari = comboUsuari.SelectedItem?.ToString();
            string servei = comboTipusServeis.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(usuari) || usuari == "No definit" ||
                string.IsNullOrEmpty(servei) || servei == "No definit")
            {
                MessageBox.Show("Selecciona un usuari i un tipis de servei vàlids.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var env = EnvLoader.Load("config.env");
                string connStr = $"Server={env["MYSQL_HOST"]};Port={env["MYSQL_PORT"]};Database={env["MYSQL_DATABASE"]};Uid={env["MYSQL_USER"]};Pwd={env["MYSQL_PASSWORD"]};";
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // obtaining the id of the selected service
                    string sqlIdServei = "SELECT idservei FROM serveis WHERE nom = @nom LIMIT 1";
                    int? idServei = null;
                    using (var cmdServei = new MySqlCommand(sqlIdServei, conn))
                    {
                        cmdServei.Parameters.AddWithValue("@nom", servei);
                        var result = cmdServei.ExecuteScalar();
                        if (result != null && int.TryParse(result.ToString(), out int id))
                            idServei = id;
                    }
                    if (idServei == null)
                    {
                        MessageBox.Show("No s'ha trobat el servei seleccionat.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // obtaining the id of the selected user
                    string sqlIdUsuari = "SELECT idusuari FROM usuaris WHERE nomcognoms = @nomcognoms LIMIT 1";
                    int? idUsuari = null;
                    using (var cmdUsuari = new MySqlCommand(sqlIdUsuari, conn))
                    {
                        cmdUsuari.Parameters.AddWithValue("@nomcognoms", usuari);
                        var result = cmdUsuari.ExecuteScalar();
                        if (result != null && int.TryParse(result.ToString(), out int id))
                            idUsuari = id;
                    }
                    if (idUsuari == null)
                    {
                        MessageBox.Show("No s'ha trobat l'usuari seleccionat.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    //update the primary service of the selected user
                    string sqlUpdateUsuari = "UPDATE usuaris SET servei_principal = @idServei WHERE idusuari = @idUsuari";
                    using (var cmdUpdateUsuari = new MySqlCommand(sqlUpdateUsuari, conn))
                    {
                        cmdUpdateUsuari.Parameters.AddWithValue("@idServei", idServei.Value);
                        cmdUpdateUsuari.Parameters.AddWithValue("@idUsuari", idUsuari.Value);
                        cmdUpdateUsuari.ExecuteNonQuery();
                    }

                    //update the user associated with the computer
                    string codiAJT = new string(Environment.MachineName.Where(char.IsDigit).ToArray());
                    string sqlUpdateElements = "UPDATE elements SET usuari = @idUsuari WHERE codiAJT = @codiAJT";
                    using (var cmdUpdateElements = new MySqlCommand(sqlUpdateElements, conn))
                    {
                        cmdUpdateElements.Parameters.AddWithValue("@idUsuari", idUsuari.Value);
                        cmdUpdateElements.Parameters.AddWithValue("@codiAJT", codiAJT);
                        cmdUpdateElements.ExecuteNonQuery();
                    }

                    MessageBox.Show("Actualització realitzada correctament!", "Informació", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error actualitzant la base de dades: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
