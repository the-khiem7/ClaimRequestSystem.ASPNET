using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
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
            var response = await _claimService.GetClaimsAsync(status);
            return Ok(ApiResponseBuilder.BuildResponse(
                message: "Get claims successfully!",
                data: response,
                statusCode: StatusCodes.Status200OK));
        }

        [HttpGet(ApiEndPointConstant.Claim.ClaimsEndpoint + "/{id}")]
        [ProducesResponseType(typeof(ViewClaimResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClaimById(Guid id)
        {
            var response = await _claimService.GetClaimByIdAsync(id);
            return Ok(ApiResponseBuilder.BuildResponse(
                message: $"Get claim with id {id} successfully!",
                data: response,
                statusCode: StatusCodes.Status200OK));
        }
    }
}
