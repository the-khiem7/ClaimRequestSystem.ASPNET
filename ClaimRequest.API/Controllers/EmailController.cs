using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using ClaimRequest.DAL.Data.Responses.Email;
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

        [HttpPost("test/send-claim-returned/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendClaimReturnedEmail(Guid claimId)
        {
            try
            {
                await _emailService.SendClaimReturnedEmail(claimId);
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

        [HttpPost("test/send-claim-submitted/{claimerId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendClaimSubmittedEmail(Guid claimerId)
        {
            try
            {
                await _emailService.SendClaimSubmittedEmail(claimerId);
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

        [HttpPost("test/send-manager-approved/{approverId}/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendManagerApprovedEmail(Guid approverId, Guid claimId)
        {
            try
            {
                await _emailService.SendManagerApprovedEmail(approverId, claimId);
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

        [HttpPost("test/send-claim-approved/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendClaimApprovedEmail(Guid claimId)
        {
            try
            {
                await _emailService.SendClaimApprovedEmail(claimId);
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
                return BadRequest(ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status400BadRequest,
                    "Invalid request",
                    "The request data is invalid."
                ));
            }

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
