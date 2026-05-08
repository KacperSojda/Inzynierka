using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using INZYNIERKA.Services.Interfaces;

namespace INZYNIERKA.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration configuration;

        public EmailService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var host = configuration["EmailConfiguration:SmtpServer"];
                var port = int.Parse(configuration["EmailConfiguration:SmtpPort"]);
                var mail = configuration["EmailConfiguration:SmtpUsername"];
                var pw = configuration["EmailConfiguration:SmtpPassword"];

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