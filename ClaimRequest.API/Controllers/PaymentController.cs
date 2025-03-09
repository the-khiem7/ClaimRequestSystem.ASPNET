using ClaimRequest.BLL.Services.Implements.VNPayService.Models;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ClaimRequest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly IGenericRepository<Claim> _claimRepository;

        public PaymentController(IVnPayService vnPayService, IGenericRepository<Claim> claimRepository)
        {
            _vnPayService = vnPayService;
            _claimRepository = claimRepository;
        }

        [HttpPost("create-payment-url")]
        public async Task<IActionResult> CreatePaymentUrl([FromQuery] Guid claimId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
                return NotFound("Invalid claim ID");

            if (claim.Status != ClaimStatus.Approved)
                return BadRequest("Only approved claims can be paid!");

            var model = new PaymentInformationModel()
            {
                ClaimId = claim.Id,
                Amount = claim.Amount,
                ClaimType = claim.ClaimType.ToString()
            };

            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Ok(new { PaymentUrl = url });
        }

        [HttpGet("payment-callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            // Access query parameters directly from HttpContext.Request.Query
            var queryParams = HttpContext.Request.Query;

            var response = _vnPayService.PaymentExecute(queryParams);

            if (response == null || response.PaymentId == null)
                return BadRequest("Payment failed");

            if (!TryParseClaimId(response.PaymentId, out Guid claimId))
                return BadRequest("Invalid payment ID");

            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return NotFound("Claim not found");
            }

            if (response.VnPayResponseCode != "00")
            {
                return BadRequest("Payment was not successful");
            }

            return Ok(new
            {
                Message = "Payment successful",
                ClaimId = claimId
            });
        }


        private bool TryParseClaimId(string paymentId, out Guid claimId)
        {
            claimId = Guid.Empty;
            var txnRefParts = paymentId.Split('-');
            return txnRefParts.Length > 0 && Guid.TryParse(txnRefParts[0], out claimId);
        }
    }
}

