using System;
using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class ClaimController : BaseController<ClaimController>
    {
        #region Create Class Referrence
        private readonly IClaimService _claimService;
        #endregion

        #region Contructor
        public ClaimController(ILogger<ClaimController> logger, IClaimService claimService) : base(logger)
        {
            _claimService = claimService;
        }
        #endregion


        [HttpPost(ApiEndPointConstant.Claim.ClaimsEndpoint)]
        [ProducesResponseType(typeof(CreateClaimResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequest createClaimRequest)
        {
            var response = await _claimService.CreateClaim(createClaimRequest);
            if (response == null)
            {
                _logger.LogError("Create claim failed");
                return Problem("Create claim failed");
            }
            return CreatedAtAction(nameof(CreateClaim), response);
        }

        [HttpGet(ApiEndPointConstant.Claim.ClaimsEndpoint)]
        [ProducesResponseType(typeof(IEnumerable<ViewClaimResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClaims([FromQuery] ClaimStatus? status)
        {
            var response = await _claimService.GetClaims(status);
            return Ok(ApiResponseBuilder.BuildResponse(
                message: "Get claims successfully!",
                data: response,
                statusCode: StatusCodes.Status200OK));
        }

        [HttpGet(ApiEndPointConstant.Claim.ClaimEndpointById)]
        [ProducesResponseType(typeof(ViewClaimResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClaimById(Guid id)
        {
            var response = await _claimService.GetClaimById(id);
            return Ok(ApiResponseBuilder.BuildResponse(
                message: $"Get claim with id {id} successfully!",
                data: response,
                statusCode: StatusCodes.Status200OK));
        }

        [HttpPut(ApiEndPointConstant.Claim.UpdateClaimEndpoint)]
        [ProducesResponseType(typeof(UpdateClaimResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateClaim(Guid id, [FromBody] UpdateClaimRequest updateClaimRequest)
        {
            try
            {
                var updatedClaim = await _claimService.UpdateClaim(id, updateClaimRequest);
                if (updatedClaim == null)
                {
                    var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status404NotFound,
                        "Claim not found",
                        "The claim ID provided does not exist or could not be updated"
                    );
                    return NotFound(errorResponse);
                }

                var successResponse = ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status200OK,
                    "Claim updated successfully",
                    updatedClaim
                );
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim with ID {ClaimId}", id);

                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while updating the claim",
                    "Internal server error"
                );
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpPut(ApiEndPointConstant.Claim.RejectClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<RejectClaimResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RejectClaim([FromRoute] Guid id, [FromBody] RejectClaimRequest rejectClaimRequest)
        {
            var rejectClaim = await _claimService.RejectClaim(id, rejectClaimRequest);
            if (rejectClaim == null)
            {
                _logger.LogError("Reject claim failed");
                return Problem("Reject claim failed");
            }

            var successResponse = ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Claim Rejected successfully",
                rejectClaim
            );
            return Ok(successResponse);
        }

        [HttpPut(ApiEndPointConstant.Claim.CancelClaimEndpoint)]
        [ProducesResponseType(typeof(CancelClaimResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelClaim([FromBody] CancelClaimRequest cancelClaimRequest)
        {
            var response = await _claimService.CancelClaim(cancelClaimRequest);
            if (response == null)
            {
                _logger.LogError("Cancel claim failed");
                return Problem("Cancel claim failed");
            }
            return Ok(response);
        }

        [HttpGet(ApiEndPointConstant.Claim.DownloadClaimEndpoint)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadClaim([FromQuery] DownloadClaimRequest downloadClaimRequest)
        {
            var stream = await _claimService.DownloadClaimAsync(downloadClaimRequest);

            if (stream == null)
            {
                _logger.LogError("Download claim failed");
                return Problem("Download claim failed");
            }

            var fileName = "Template_Export_Claim.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPut(ApiEndPointConstant.Claim.ApproveClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<ApproveClaimResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveClaim([FromRoute] Guid id, [FromRoute] Guid approverId, [FromBody] ApproveClaimRequest approveClaimRequest)
        {
            try
            {
                var response = await _claimService.ApproveClaim(id, approverId, approveClaimRequest);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                _logger.LogError(ex, "Approve claim failed: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                _logger.LogError(ex, "Approve claim failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving claim with ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
