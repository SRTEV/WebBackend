using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace TransportApi.Services
{
    public class EmailService : IEmailService 
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendResetPasswordEmail(string email, string resetLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SRTEV", _config["EMAIL_FROM"]));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Password Reset";

            var builder = new BodyBuilder
            {
                HtmlBody = $"<h1>Password Reset Request</h1>" +
                           $"<p>Click the link below to change your password:</p>" +
                          $"<a href='http://10.0.2.2:5194/GoToApp.html?token={resetLink}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Reset Password</a>"
            };
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_config["EMAIL_USERNAME"], _config["EMAIL_PASSWORD"]);
                    await client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw new Exception($"SMTP error: {ex.Message}", ex);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

        public async Task SendReportReplyEmail(string email, string subject, string messageText)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SRTEV", _config["EMAIL_FROM"]));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                TextBody = messageText
            };
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {

                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    
                    await client.ConnectAsync("smtp.ethereal.email", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_config["EMAIL_USERNAME"], _config["EMAIL_PASSWORD"]);
                    await client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending report reply email: {ex.Message}");
                    throw new Exception($"SMTP error while replying to report: {ex.Message}", ex);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}