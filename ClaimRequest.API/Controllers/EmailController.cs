using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Requests.Email;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class EmailController : BaseController<EmailController>
    {
        private readonly IEmailService _emailService;

        public EmailController(ILogger<EmailController> logger, IEmailService emailService) : base(logger)
        {
            _emailService = emailService;
        }

        [HttpPost(ApiEndPointConstant.Email.SendEmail)]
        public async Task<IActionResult> SendEmail([FromBody] SendMailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailService.SendEmailAsync(request);
            if (!result)
            {
                return StatusCode(500, "Failed to send email.");
            }

            return Ok("Email sent successfully.");
        }
    }
}
