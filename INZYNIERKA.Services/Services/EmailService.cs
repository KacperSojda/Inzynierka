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

        /// <summary>Sends an email with an HTML body using configured SMTP settings.</summary>
        /// <param name="email">The email address.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="htmlMessage">The HTML-formatted body of the email.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
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