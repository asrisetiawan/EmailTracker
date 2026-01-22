namespace EmailTracker.Middleware
{
    public class IpWhitelistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _whitelistedIPs;
        public IpWhitelistMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _whitelistedIPs = config.GetSection("WhitelistedIPs").Get<List<string>>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (!_whitelistedIPs.Contains(ip)) { context.Response.StatusCode = 403; return; }
            await _next(context);
        }
    }
}
