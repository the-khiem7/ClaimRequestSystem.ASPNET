using ClaimRequest.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("send-claim-returned/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendClaimReturnedEmail(Guid claimId)
        {
            try
            {
                await _emailService.SendClaimReturnedEmail(claimId);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [HttpPost("send-claim-submitted/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendClaimSubmittedEmail(Guid claimId)
        {
            try
            {
                await _emailService.SendClaimSubmittedEmail(claimId);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("send-manager-approved/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendManagerApprovedEmail([FromRoute] Guid claimId)
        {
            try
            {
                var approverIdClaim = User.FindFirst("StaffId")?.Value;
                if (string.IsNullOrEmpty(approverIdClaim))
                {
                    return Unauthorized("Approver ID not found in token.");
                }

                var approverId = Guid.Parse(approverIdClaim);
                await _emailService.SendManagerApprovedEmail(approverId, claimId);

                return Ok(new { message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }


        [HttpPost("send-claim-approved/{claimId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendClaimApprovedEmail(Guid claimId)
        {
            try
            {
                await _emailService.SendClaimApprovedEmail(claimId);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to send email.", details = ex.Message });
            }
        }
    }
}
