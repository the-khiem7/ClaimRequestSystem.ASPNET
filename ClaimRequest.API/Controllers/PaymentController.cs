using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Implements.VNPayService.Models;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class PaymentController : BaseController<PaymentController>
    {
        private readonly IVnPayService _vnPayService;
        private readonly IGenericRepository<Claim> _claimRepository;
        private readonly IClaimService _claimService;
        private readonly IConfiguration _configuration;

        public PaymentController(ILogger<PaymentController> logger, IVnPayService vnPayService, IGenericRepository<Claim> claimRepository, IClaimService claimService, IConfiguration configuration) : base(logger)
        {
            _vnPayService = vnPayService;
            _claimRepository = claimRepository;
            _claimService = claimService;
            _configuration = configuration;
        }

        [HttpPost(ApiEndPointConstant.Payment.CreatePaymentUrl)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePaymentUrl([FromQuery] List<Guid> claimIds, Guid financeId)
        {
            if (!claimIds.Any())
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "At least one claim must be selected",
                    null));

            var validClaims = new List<Claim>();
            foreach (var claimId in claimIds)
            {
                var claim = await _claimRepository.GetByIdAsync(claimId);
                if (claim == null || claim.Status != ClaimStatus.Approved)
                {
                    return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                        StatusCodes.Status400BadRequest,
                        $"Claim {claimId} is not valid or not approved",
                        null));
                }
                validClaims.Add(claim);
            }

            var model = new PaymentInformationModel()
            {
                FinanceId = financeId,
                ClaimIds = validClaims.Select(c => c.Id).ToList(),
                Amount = validClaims.Sum(c => c.Amount),
                ClaimType = string.Join(",", validClaims.Select(c => c.ClaimType.ToString()))
            };

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Payment URL created successfully",
                new { PaymentUrl = url }));
        }

        [HttpGet(ApiEndPointConstant.Payment.PaymentCallback)]
        public async Task<IActionResult> PaymentCallback()
        {
            var queryParams = HttpContext.Request.Query;
            if (queryParams.Count == 0)
                return BadRequest("No query parameters provided");

            var response = _vnPayService.PaymentExecute(queryParams);
            if (response == null || !response.Success)
                return BadRequest("Payment failed");

            var orderInfo = queryParams["vnp_OrderInfo"].ToString();
            if (string.IsNullOrEmpty(orderInfo))
                return BadRequest("Missing order info");

            var model = JsonSerializer.Deserialize<PaymentInformationModel>(orderInfo);
            if (model == null || !model.ClaimIds.Any())
                return BadRequest("Invalid payment details");

            if (response.VnPayResponseCode != "00")
                return Redirect($"{_configuration["Vnpay:ReturnUrlResult"]}?status=failed&errorCode={response.VnPayResponseCode}");

            foreach (var claimId in model.ClaimIds)
            {
                var paidResult = await _claimService.PaidClaim(claimId, model.FinanceId);
                if (!paidResult)
                    return Redirect($"{_configuration["Vnpay:ReturnUrlResult"]}?status=failed&errorMessage=Failed to update claim {claimId} as paid");
            }

            return Redirect($"{_configuration["Vnpay:ReturnUrlResult"]}?status=success");
        }
    }
}
