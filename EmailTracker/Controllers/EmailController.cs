using System.Threading.Tasks;
using EmailTracker.Domain.Services;
using EmailTracker.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using EmailTracker.Domain.Request;

namespace EmailTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody]EmailRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            var result = await _emailService.SendEmailAsync(request);
            return Ok(new { MessageId = result });
        }
    }
}