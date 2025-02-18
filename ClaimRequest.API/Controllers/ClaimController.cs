using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
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

        [HttpPut(ApiEndPointConstant.Claim.ApproveClaimEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<ApproveClaimResponse>), StatusCodes.Status200OK)]  
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApproveClaim(Guid Id, [FromBody] ApproveClaimRequest approveClaimRequest)
        {
            try
            {
                var approveClaim = await _claimService.ApproveClaim(Id, approveClaimRequest);
                if (approveClaim == null)
                {
                    var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                            null,
                            StatusCodes.Status404NotFound,
                            "Claim not found",
                            "The claim ID provided does not exist or is not pending for approve"
                            );
                    return NotFound(errorResponse);
                }

                var successResponse = ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status200OK,
                    "Claim Approved successfully",
                    approveClaim 
                );
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approve claim with ID {ClaimId}", Id);

                var errorResponse = ApiResponseBuilder.BuildErrorResponse<object>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while approve the claim",
                    "Internal server error"
                );
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }

        }
    }
}
