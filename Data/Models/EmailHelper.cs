using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailHelper
{
    public static async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var fromEmail = "mohamed.0523081@gmail.com"; 
        var password = "fnqh siaa iaka mace";

        var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(fromEmail, password)
        };

        var message = new MailMessage(fromEmail, toEmail, subject, body)
        {
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(message);
    }
}
