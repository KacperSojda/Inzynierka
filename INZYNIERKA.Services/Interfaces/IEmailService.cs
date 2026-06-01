namespace INZYNIERKA.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmail(string email, string subject, string htmlMessage);
    }
}
