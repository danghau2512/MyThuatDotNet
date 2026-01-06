using System.Net;
using System.Net.Mail;

namespace MyThuatShop.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    public SmtpEmailSender(IConfiguration config) => _config = config;

    public async Task SendHtmlAsync(string toEmail, string subject, string htmlBody)
    {
        // appsettings.json: Smtp:Host, Port, User, Pass, From
        var host = _config["Smtp:Host"];
        var user = _config["Smtp:User"];
        var pass = _config["Smtp:Pass"];
        var from = _config["Smtp:From"] ?? user;
        var portStr = _config["Smtp:Port"];
        _ = int.TryParse(portStr, out var port);
        if (port <= 0) port = 587;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP chưa cấu hình (Smtp:Host / Smtp:From).");

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = true
        };

        if (!string.IsNullOrWhiteSpace(user))
            smtp.Credentials = new NetworkCredential(user, pass);

        var mail = new MailMessage(from, toEmail, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(mail);
    }
}
