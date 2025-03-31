using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ClaimRequest.API.Constants;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Data.Exceptions;

namespace ClaimRequest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        public readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("test/send-claim-returned/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendClaimReturnedEmail(Guid id)
        {
            try
            {
                await _emailService.SendClaimReturnedEmail(id);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [HttpPost("test/send-claim-submitted/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendClaimSubmittedEmail(Guid id)
        {
            try
            {
                await _emailService.SendClaimSubmittedEmail(id);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [HttpPost("test/send-manager-approved/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendManagerApprovedEmail(Guid id)
        {
            try
            {
                await _emailService.SendManagerApprovedEmail(id);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [HttpPost("test/send-claim-approved/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendClaimApprovedEmail(Guid id)
        {
            try
            {
                await _emailService.SendClaimApprovedEmail(id);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [HttpPost(ApiEndPointConstant.Email.SendOtp)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpEmailRequest request)
        {
            try
            {
                var response = await _emailService.SendOtpEmailAsync(request);
                return Ok(ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status200OK,
                    "OTP sent successfully.",
                    response
                ));
            }
            catch (NotFoundException ex)
            {
                return NotFound(ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status404NotFound,
                    $"Staff with email {request.Email} not found.",
                    ex.Message
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "Failed to send OTP email.",
                    ex.Message
                ));
            }
        }
    }
}
