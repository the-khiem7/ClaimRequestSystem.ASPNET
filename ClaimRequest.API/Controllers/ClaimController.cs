using ClaimRequest.API.Constants;
using ClaimRequest.API.Extensions;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ClaimRequest.API.Controllers
{
    [ApiController]
    [Authorize(Policy = "RequireAnyRole")]
    public class ClaimController : BaseController<ClaimController>
    {
        #region Create Class Referrence
        private readonly IClaimService _claimService;
        private readonly IEmailService _emailService;
        #endregion

        #region Contructor
        public ClaimController(ILogger<ClaimController> logger, IClaimService claimService, IEmailService emailService) : base(logger)
        {
            _claimService = claimService;
            _emailService = emailService;
        }
        #endregion

        [Authorize(Policy = "CanCreateClaim")]
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

        [Authorize(Policy = "CanViewClaims")]
        [HttpGet(ApiEndPointConstant.Claim.ClaimsEndpoint)]
        [ProducesResponseType(typeof(IEnumerable<ViewClaimResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClaims(
            [FromQuery] ClaimStatus? status,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] ClaimService.ViewMode viewMode = ClaimService.ViewMode.ClaimerMode,
            [FromQuery] string sortBy = "id",
            [FromQuery] bool descending = false
            )
        {

            var response = await _claimService.GetClaims(pageNumber, pageSize, status, viewMode.ToString(), search, sortBy, descending, fromDate, toDate);

            return Ok(ApiResponseBuilder.BuildResponse(
                message: "Get claims successfully!",
                data: response,
                statusCode: StatusCodes.Status200OK));
        }


        [Authorize(Policy = "CanViewClaims")]
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

        [Authorize(Policy = "CanUpdateClaim")]
        [HttpPut(ApiEndPointConstant.Claim.UpdateClaimEndpoint)]
        [ProducesResponseType(typeof(UpdateClaimResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateClaim([FromRoute] Guid id, [FromBody] UpdateClaimRequest updateClaimRequest)
        {
            try
            {
                var updatedClaim = await _claimService.UpdateClaim(id, updateClaimRequest);
                if (updatedClaim == null)
                {
                    var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                        new object(), // Provide a non-null object
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
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Claim with ID {ClaimId} not found", id);
                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    new object(), // Provide a non-null object
                    StatusCodes.Status404NotFound,
                    "Claim not found",
                    ex.Message
                );
                return NotFound(errorResponse);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation for claim with ID {ClaimId}", id);
                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    new object(), // Provide a non-null object
                    StatusCodes.Status400BadRequest,
                    "Invalid operation",
                    ex.Message
                );
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim with ID {ClaimId}", id);
                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    new object(), // Provide a non-null object
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while updating the claim",
                    "Internal server error"
                );
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }


        [Authorize(Policy = "CanRejectClaim")]
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

        [Authorize(Policy = "CanCancelClaim")]
        [HttpPut(ApiEndPointConstant.Claim.CancelClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<CancelClaimResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelClaim([FromRoute] Guid claimId, [FromBody] CancelClaimRequest cancelClaimRequest)
        {
            var response = await _claimService.CancelClaim(claimId, cancelClaimRequest);
            if (response == null)
            {
                _logger.LogError("Cancel claim failed");
                return Problem("Cancel claim failed");
            }
            var successRespose = ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Claim canceled successfully!",
                response);
            return Ok(successRespose);
        }

        [Authorize(Policy = "CanDownloadClaim")]
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

        [Authorize(Policy = "CanApproveClaim")]
        [HttpPut(ApiEndPointConstant.Claim.ApproveClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveClaim([FromRoute] Guid id)
        {
            var result = await _claimService.ApproveClaim(User, id);
            if (result == null)
            {
                _logger.LogError("Approve claim failed");
                return Problem("Approve claim failed");
            }
            try
            {
                await _emailService.SendClaimApprovedEmail(id);
                await _emailService.SendManagerApprovedEmail(id);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send approval email for Claim ID: {ClaimId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Claim approved but email failed",
                    error = emailEx.Message
                });
            }
            var successRespose = ApiResponseBuilder.BuildResponse(
                message: "Claim approved successfully!",
                data: result,
                statusCode: StatusCodes.Status200OK);
            return Ok(successRespose);
        }

        [Authorize(Policy = "CanReturnClaim")]
        [HttpPut(ApiEndPointConstant.Claim.ReturnClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<ReturnClaimResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReturnClaim(Guid id, [FromBody] ReturnClaimRequest returnClaimRequest)
        {
            try
            {
                var returnedClaim = await _claimService.ReturnClaim(id, returnClaimRequest);
                if (returnedClaim == null)
                {
                    var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status404NotFound,
                        "Claim not found",
                        "The claim ID provided does not exist or is not pending for return"
                    );
                    return NotFound(errorResponse);
                }

                await _emailService.SendClaimReturnedEmail(id);
                var successResponse = ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status200OK,
                    "Claim returned successfully",
                    returnedClaim
                );
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning claim with ID {ClaimId}", id);

                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while returning the claim",
                    "Internal server error"
                );
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [Authorize(Policy = "CanSubmitClaim")]
        [HttpPut(ApiEndPointConstant.Claim.SubmitClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitClaim(Guid id)
        {
            try
            {
                var result = await _claimService.SubmitClaim(id);
                if (!result)
                {
                    _logger.LogError("Submit claim failed");
                    return NotFound(new { message = "Submit claim failed" });
                }

                await _emailService.SendClaimSubmittedEmail(id);
                var successResponse = ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status200OK,
                    "Claim submitted successfully",
                    result
                );

                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting claim with ID {ClaimId}", id);

                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while submitting the claim",
                    "Internal server error"
                );
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [Authorize(Policy = "CanProcessPayment")]
        [HttpPut(ApiEndPointConstant.Claim.PaidClaimEndpoint)]
        [ProducesResponseType(typeof(CreateClaimResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ValidateModelAttributes]
        public async Task<IActionResult> PayClaim([FromRoute] Guid id, [FromQuery] Guid financeId)
        {
            var response = await _claimService.PaidClaim(id, financeId);

            return Ok(ApiResponseBuilder.BuildResponse(
                message: "Claim paid successfully.",
                data: response,
                statusCode: StatusCodes.Status200OK
            ));
        }
    }
}