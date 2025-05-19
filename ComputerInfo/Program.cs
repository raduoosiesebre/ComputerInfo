using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputerInfo
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayAppContext());
        }
    }

    public class TrayAppContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public TrayAppContext()
        {
            string computerName = Environment.MachineName;
            string osInfo = $"{Environment.OSVersion} (64-bit: {Environment.Is64BitOperatingSystem})";
            string trayText = $"PC: {computerName} | OS Info: {osInfo}";

            trayText = trayText.Replace("\n", " ").Replace("\r", "");
            if (trayText.Length > 63)
                trayText = trayText.Substring(0, 63);

            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = trayText,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Exit", (s, e) => ExitThread())
                })
            };

            trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                string computerName = Environment.MachineName;
                string osInfo = $"{Environment.OSVersion} (64-bit: {Environment.Is64BitOperatingSystem})";
                var infoForm = new InfoForm(computerName, osInfo);
                infoForm.ShowDialog();
            }
        }

        protected override void ExitThreadCore()
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.ExitThreadCore();
        }
    }
}
