using EmailTracker.Domain.Request;

namespace EmailTracker.Domain.Services
{
    public interface IEmailService
    {
        Task<string> SendEmailAsync(EmailRequest request);
    }
}