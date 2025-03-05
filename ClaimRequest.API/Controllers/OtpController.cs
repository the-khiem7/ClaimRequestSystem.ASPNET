using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Requests.Otp;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class OtpController : BaseController<OtpController>
    {
        private readonly IOtpService _otpService;

        public OtpController(ILogger<OtpController> logger, IOtpService otpService) : base(logger)
        {
            _otpService = otpService;
        }

        [HttpPost(ApiEndPointConstant.Otp.ValidateOtp)]
        public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _otpService.ValidateOtp(request.Email, request.Otp);

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
    }
}
