using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmail(string email, string subject, string htmlMessage)
        {
            try
            {
                var host = _configuration["EmailConfiguration:SmtpServer"];
                var port = int.Parse(_configuration["EmailConfiguration:SmtpPort"]);
                var mail = _configuration["EmailConfiguration:SmtpUsername"];
                var pw = _configuration["EmailConfiguration:SmtpPassword"];

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(mail, pw)
                };

                var mailMessage = new MailMessage(
                    from: mail,
                    to: email,
                    subject,
                    htmlMessage
                )
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
    }
}