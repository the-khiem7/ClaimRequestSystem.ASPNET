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
        private readonly IClaimService _claimService;

        public PaymentController(ILogger<PaymentController> logger, IVnPayService vnPayService, IGenericRepository<Claim> claimRepository, IClaimService claimService) : base(logger)
        {
            _vnPayService = vnPayService;
            _claimRepository = claimRepository;
            _claimService = claimService;
        }

        [HttpPost(ApiEndPointConstant.Payment.CreatePaymentUrl)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreatePaymentUrl([FromQuery] Guid claimId, Guid financeId)
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
                FinanceId = financeId,
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
            Console.WriteLine("Payment callback reached!");
            var queryParams = HttpContext.Request.Query;

            if (queryParams.Count == 0)
            {
                Console.WriteLine("No query parameters received in payment callback.");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "No query parameters provided",
                    null));
            }

            Console.WriteLine($"queryParams={queryParams}");

            var response = _vnPayService.PaymentExecute(queryParams);
            if (response == null || !response.Success)
            {
                Console.WriteLine($"Payment execution failed. Response: {response}");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Payment failed",
                    null));
            }

            var txnRef = queryParams["vnp_TxnRef"].ToString();
            if (string.IsNullOrEmpty(txnRef) || txnRef.Length < 32 || !TryParseClaimId(txnRef.Substring(0, 32), out Guid claimId))
            {
                Console.WriteLine($"Invalid or missing transaction reference: {txnRef}");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Invalid transaction reference",
                    null));
            }

            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                Console.WriteLine($"Claim not found for ClaimId: {claimId}");
                return NotFound(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status404NotFound,
                    "Claim not found",
                    null));
            }

            if (!queryParams.ContainsKey("vnp_OrderInfo") || string.IsNullOrEmpty(queryParams["vnp_OrderInfo"]))
            {
                Console.WriteLine("Missing or empty vnp_OrderInfo in query parameters.");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Missing order info",
                    null));
            }

            var orderInfo = queryParams["vnp_OrderInfo"].ToString();
            var decodedOrderInfo = Uri.UnescapeDataString(orderInfo);
            var infoParts = decodedOrderInfo.Split('|', StringSplitOptions.RemoveEmptyEntries);

            var financePart = infoParts.FirstOrDefault(p => p.StartsWith("FinanceId="));
            if (financePart == null || !Guid.TryParse(financePart.Split('=', 2)[1], out Guid financeId))
            {
                Console.WriteLine($"Invalid or missing FinanceId in order info: {decodedOrderInfo}");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Invalid FinanceId",
                    null));
            }

            if (response.VnPayResponseCode != "00")
            {
                Console.WriteLine($"Payment failed with VNPay response code: {response.VnPayResponseCode}");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Failed to update claim as paid",
                    null));
            }

            Console.WriteLine($"Calling PaidClaim with ClaimId={claimId}, FinanceId={financeId}");
            var paidResult = await _claimService.PaidClaim(claimId, financeId);
            if (!paidResult)
            {
                Console.WriteLine($"Failed to mark claim as paid. ClaimId: {claimId}, FinanceId: {financeId}");
                return BadRequest(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status400BadRequest,
                    "Failed to update claim as paid",
                    null));
            }

            Console.WriteLine($"Payment processed successfully. ClaimId: {claimId}, FinanceId: {financeId}");
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Payment successful",
                new { ClaimId = claimId, FinanceId = financeId }));
        }

        private bool TryParseClaimId(string paymentId, out Guid claimId)
        {
            claimId = Guid.Empty;
            var txnRefParts = paymentId.Split('-');
            return txnRefParts.Length > 0 && Guid.TryParse(txnRefParts[0], out claimId);
        }
    }
}

