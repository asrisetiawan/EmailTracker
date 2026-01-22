using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Concurrent;
using EmailTracker.Domain.Services;
using EmailTracker.Domain.Request;

namespace EmailTracker.Infrastructure.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }



        // Function to send email asynchronously and track status
        public async Task<string> SendEmailAsync(EmailRequest request)
        {
            var smtpConfig = _configuration.GetSection("Smtp");
            string smtpHost = smtpConfig["Host"] ?? "smtp.gmail.com";
            int smtpPort = int.Parse(smtpConfig["Port"] ?? "587");
            string smtpUser = smtpConfig["Username"] ?? "";
            string smtpPass = smtpConfig["Password"] ?? "";
            bool enableSsl = bool.Parse(smtpConfig["EnableSsl"] ?? "true");

            // In-memory tracking store: Dictionary<trackingId, (status, reason)>
            var trackingStore = new ConcurrentDictionary<string, (string Status, string? Reason)>();
            var trackingId = Guid.NewGuid().ToString();
            trackingStore[trackingId] = ("pending", null);

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                await client.AuthenticateAsync(smtpUser, smtpPass);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Your Service", smtpUser));
                message.To.Add(new MailboxAddress("", request.To));
                message.Subject = request.Subject;
                message.Body = new TextPart("plain") { Text = request.Body };

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                trackingStore[trackingId] = ("sent", null);
            }
            catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
            {
                // 550: Invalid email
                trackingStore[trackingId] = ("failed", "invalid email");
                throw;
            }
            catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.TransactionFailed)
            {
                // 552: Mailbox full
                trackingStore[trackingId] = ("failed", "mailbox full");
                throw;
            }
            catch (Exception)
            {
                // Generic failure
                trackingStore[trackingId] = ("failed", "unknown error");
                throw;
            }

            return trackingId;
        }
    }
}
