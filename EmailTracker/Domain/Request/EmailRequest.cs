namespace EmailTracker.Domain.Request
{
    public record EmailRequest(string To, string Subject, string Body);
}