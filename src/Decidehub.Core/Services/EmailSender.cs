using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Decidehub.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Decidehub.Core.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        private readonly IHostingEnvironment _env;
        private readonly IConfiguration _configuration;

        public EmailSender(IHostingEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message, string tenant)
        {
            await Execute(email, subject, message, tenant);
        }

        private async Task Execute(string email, string subject, string message, string tenant)
        {
            var name = string.IsNullOrWhiteSpace(tenant) ? "Decidehub" : $"{tenant}.decidehub.com";

            var apiKey = _configuration["SendGridApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("info@decidehub.com", name);
            var to = new EmailAddress(email);
            var plainTextContent = Regex.Replace(message, "<[^>]*>", "");
            var path = _env.WebRootPath;
            if (_env.WebRootPath == null)
                path = AppDomain.CurrentDomain.BaseDirectory;
            var body = await File.ReadAllTextAsync(path + "/EmailTemplates/mail.html");
            body = body.Replace("#MailContent#", message);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, body);
            await client.SendEmailAsync(msg);
        }
    }
}