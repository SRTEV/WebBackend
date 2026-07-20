namespace TransportApi.Services
{
    public interface IEmailService
    {
        Task SendResetPasswordEmail(string email, string resetLink);
        Task SendReportReplyEmail(string email, string subject, string messageText);
    }
}