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
                          $"<a href='http://172.30.204.198:5194/GoToApp.html?token={resetLink}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Reset Password</a>"
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
    
                    // Handle exception (e.g., log it)
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw new Exception($"Помилка SMTP: {ex.Message}", ex);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}