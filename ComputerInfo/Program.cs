using ComputerInfo;
using System;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ComputerInfo
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            string computerName = Environment.MachineName;
            string ip = GetLocalIP();

            if (!IsPCNameValid(computerName))
            {
                SendEmail(computerName, ip);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayAppContext());
        }

        static bool IsPCNameValid(string computerName)
        {
            return Regex.IsMatch(computerName, @"^WS\d{4}$");
        }

        static void SendEmail(string nombrePC, string ip)
        {
            try
            {
                string envPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.env");
                var env = EnvLoader.Load(envPath);
                var smtp = new System.Net.Mail.SmtpClient(env["SMTP_HOST"], int.Parse(env["SMTP_PORT"]))
                {
                    Credentials = new System.Net.NetworkCredential(env["SMTP_USER"], env["SMTP_PASS"]),
                    EnableSsl = true
                };

                var mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress(env["SMTP_FROM"]);
                mail.To.Add(env["SMTP_TO"]);
                mail.Subject = "Nom de PC incorrecte";
                mail.Body = $"Nom de PC: {nombrePC}\nIP: {ip}";

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error enviant l'email d'avís: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static string GetLocalIP()
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
        if (e.Button == MouseButtons.Left)
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