using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System.Collections.Concurrent;

namespace EmailTracker.Infrastructure.Email
{

    public class BounceMonitorService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BounceMonitorService> _logger;
        //private readonly ConcurrentDictionary<string, (string Status, string? Reason)> _trackingStore;

        public BounceMonitorService(IConfiguration configuration, ILogger<BounceMonitorService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        //public BounceMonitorService(IConfiguration configuration, ILogger<BounceMonitorService> logger, ConcurrentDictionary<string, (string Status, string? Reason)> trackingStore)
        //{
        //    _configuration = configuration;
        //    _logger = logger;
        //    _trackingStore = trackingStore;
        //}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var imapConfig = _configuration.GetSection("Imap");
            string imapHost = imapConfig["Host"] ?? "imap.gmail.com";
            int imapPort = int.Parse(imapConfig["Port"] ?? "993");
            string imapUser = imapConfig["Username"] ?? "";
            string imapPass = imapConfig["Password"] ?? "";
            bool enableSsl = bool.Parse(imapConfig["EnableSsl"] ?? "true");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var client = new ImapClient();
                    await client.ConnectAsync(imapHost, imapPort, true);
                    await client.AuthenticateAsync(imapUser, imapPass);

                    var inbox = client.Inbox;
                    await inbox.OpenAsync(FolderAccess.ReadWrite);

                    // Search for unread emails (bounces often come as "Delivery Status Notification")
                    var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains("Delivery Status Notification"));
                    var uids = await inbox.SearchAsync(query);

                    foreach (var uid in uids)
                    {
                        var message = await inbox.GetMessageAsync(uid);
                        var body = message.TextBody ?? message.HtmlBody;

                        // Parse for tracking ID (assume it's in the body or subject; customize parsing)
                        var trackingId = ExtractTrackingId(body); // Implement this method
                        if (!string.IsNullOrEmpty(trackingId) )
                        {
                            // Check for hard bounce indicators (e.g., "550" or "User unknown")
                            if (body.Contains("550") || body.Contains("User unknown") || body.Contains("hard bounce"))
                            {
                                //_trackingStore[trackingId] = ("failed", "hard bounce");
                                _logger.LogWarning($"Hard bounce detected for Tracking ID: {trackingId}");
                            }
                            // Mark as read
                            await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                        }
                        //if (!string.IsNullOrEmpty(trackingId) && _trackingStore.ContainsKey(trackingId))
                        //{
                        //    // Check for hard bounce indicators (e.g., "550" or "User unknown")
                        //    if (body.Contains("550") || body.Contains("User unknown") || body.Contains("hard bounce"))
                        //    {
                        //        _trackingStore[trackingId] = ("failed", "hard bounce");
                        //        _logger.LogWarning($"Hard bounce detected for Tracking ID: {trackingId}");
                        //    }
                        //    // Mark as read
                        //    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                        //}
                    }

                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error monitoring bounces: {ex.Message}");
                }

                // Poll every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private string? ExtractTrackingId(string body)
        {
            // Simple regex or string search for tracking ID in bounce body
            // E.g., look for "Tracking ID: some-guid"
            var match = System.Text.RegularExpressions.Regex.Match(body, @"Tracking ID:\s*([a-f0-9\-]+)");
            return match.Success ? match.Groups[1].Value : null;
        }
    }

}

