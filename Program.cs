using System;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;

static class Program
{
    [STAThread]
    static void Main()
    {
        string computerName = Environment.MachineName;
        string ip = ObtenerIpLocal();

        if (!EsNombrePCValido(computerName))
        {
            EnviaEmailAviso(computerName, ip);
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InfoForm(computerName, ObtenerOSInfo()));
    }

    static bool EsNombrePCValido(string computerName)
    {
        return Regex.IsMatch(computerName, @"^WS\d{4}$");
    }

    static string ObtenerIpLocal()
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

    static void EnviaEmailAviso(string nombrePC, string ip)
    {
        try
        {
            var envPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.env");
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
            // Puedes loguear el error si lo deseas
        }
    }

    static string ObtenerOSInfo()
    {
        return Environment.OSVersion.ToString();
    }
}