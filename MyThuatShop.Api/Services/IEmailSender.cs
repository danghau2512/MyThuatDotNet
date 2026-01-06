namespace MyThuatShop.Api.Services;

public interface IEmailSender
{
    Task SendHtmlAsync(string toEmail, string subject, string htmlBody);
}
