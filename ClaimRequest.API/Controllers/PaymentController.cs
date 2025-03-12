using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Implements.VNPayService.Models;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class PaymentController : BaseController<PaymentController>
    {
        private readonly IVnPayService _vnPayService;
        private readonly IGenericRepository<Claim> _claimRepository;

        public PaymentController(ILogger<PaymentController> logger, IVnPayService vnPayService, IGenericRepository<Claim> claimRepository) : base(logger)
        {
            _vnPayService = vnPayService;
            _claimRepository = claimRepository;
        }

        [HttpPost(ApiEndPointConstant.Payment.CreatePaymentUrl)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreatePaymentUrl([FromQuery] Guid claimId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Invalid model state",
                    null,
                    ModelState.ToString()));
            }

            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
                return NotFound(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status404NotFound,
                    "Invalid claim ID",
                    null));

            if (claim.Status != ClaimStatus.Approved)
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Only approved claims can be paid!",
                    null));

            var model = new PaymentInformationModel()
            {
                ClaimId = claim.Id,
                Amount = claim.Amount,
                ClaimType = claim.ClaimType.ToString()
            };

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Payment URL created successfully",
                new { PaymentUrl = url }));
        }

        [HttpGet(ApiEndPointConstant.Payment.PaymentCallback)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PaymentCallback()
        {
            var queryParams = HttpContext.Request.Query;

            var response = _vnPayService.PaymentExecute(queryParams);

            if (response == null || response.PaymentId == null)
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Payment failed",
                    null));

            if (!TryParseClaimId(response.PaymentId, out Guid claimId))
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Invalid payment ID",
                    null));

            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status404NotFound,
                    "Claim not found",
                    null));
            }

            if (response.VnPayResponseCode != "00")
            {
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Payment was not successful",
                    null));
            }

            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Payment successful",
                new { ClaimId = claimId }));
        }

        private bool TryParseClaimId(string paymentId, out Guid claimId)
        {
            claimId = Guid.Empty;
            var txnRefParts = paymentId.Split('-');
            return txnRefParts.Length > 0 && Guid.TryParse(txnRefParts[0], out claimId);
        }
    }
}

