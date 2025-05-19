using Microsoft.Win32;
using System;
using System.Windows.Forms;
using ComputerInfo.Properties;

namespace ComputerInfo
{
    class InfoForm : Form
    {
        private TabControl tabControl;
        private TabPage infoTab;
        private TabPage settingsTab;
        private CheckBox chkAutoStart;
        private TextBox txtboxNom;
        private TextBox txtboxCognoms;
        private ComboBox comboTipusServeis;
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
            comboTipusServeis.Items.AddRange(new string[] { "No definit", "Arxiu", "Baixa", "Informàtica" });

            if(!string.IsNullOrEmpty(Properties.Settings.Default.TipusServei) &&
                comboTipusServeis.Items.Contains(Properties.Settings.Default.TipusServei))
            {
                comboTipusServeis.SelectedItem = Properties.Settings.Default.TipusServei;
            }
            else
            {
                comboTipusServeis.SelectedIndex = 0; // Default to the first item
            }

            settingsTab.Controls.Add(comboTipusServeis);

            tabControl.TabPages.Add(infoTab);
            tabControl.TabPages.Add(settingsTab);

            this.Controls.Add(tabControl);
        }

        private void ChkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            SetAutoStart(chkAutoStart.Checked);
        }

        private bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue(Application.ProductName) != null;
            }
        }

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
