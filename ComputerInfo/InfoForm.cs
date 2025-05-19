using System;
using System.Windows.Forms;

namespace ComputerInfo
{
    class InfoForm : Form
    {
        public InfoForm(string computerName, string osInfo)
        {
            this.Text = "Informació de l'ordinador";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox= false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(300, 200);

            var label = new Label
            {
                AutoSize = true,
                Location = new System.Drawing.Point(10, 10),
                Text = $"Nom de l'ordinador: {computerName}\n" +
                       $"Sistema Operatiu: {osInfo}"
            };
            this.Controls.Add(label);
        }
    }
}
