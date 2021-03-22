using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using OwnID.Extensibility.Configuration;
using OwnID.Extensibility.Services;

namespace OwnID.Services
{
    public class EmailService : IEmailService
    {
        private readonly ISmtpConfiguration _configuration;

        public EmailService(ISmtpConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task SendAsync(string toAddress, string subject, string body, bool isHtml = false, string toName = "")
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_configuration.FromName, _configuration.FromAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;
            message.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain)
            {
                Text = body
            };
            
            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration.Host, _configuration.Port, _configuration.UseSsl);
            await client.AuthenticateAsync(_configuration.UserName, _configuration.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}