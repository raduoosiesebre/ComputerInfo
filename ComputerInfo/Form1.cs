using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerInfo
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        public Form1()
        {
            InitializeComponent();

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Information;
            trayIcon.Visible = true;

            string computerName = Environment.MachineName;
            string osInfo = $"{Environment.OSVersion.VersionString} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}";
            string trayText = $"PC: {computerName} | OS Info: {osInfo}";

            // Limita a 63 caràcters
            if (trayText.Length > 63)
                trayText = trayText.Substring(0, 63);

            trayIcon.Text = trayText;

            trayIcon.BalloonTipTitle = "Dades de l'ordinador";
            trayIcon.BalloonTipText = $"Nom: {computerName}\nSO: {osInfo}";
            trayIcon.ShowBalloonTip(3000);

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Exit", (s, e) => Application.Exit());
            trayIcon.ContextMenu = contextMenu;

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            trayIcon.Visible = false; // Hide tray icon when closing the form
            base.OnFormClosing(e);
        }
    }
}
