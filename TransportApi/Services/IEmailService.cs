namespace TransportApi.Services
{
    public interface IEmailService
    {
        Task SendResetPasswordEmail(string email, string resetLink);
    }
}