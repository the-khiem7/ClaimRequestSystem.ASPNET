using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [Route(ApiEndPointConstant.Remnider.ReminderEndpoint)]
    [ApiController]
    public class PasswordReminderController : ControllerBase
    {
        private readonly IPasswordReminderService _passwordReminderService;
        private readonly ILogger<PasswordReminderController> _logger;

        public PasswordReminderController(IPasswordReminderService passwordReminderService, ILogger<PasswordReminderController> logger)
        {
            _passwordReminderService = passwordReminderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendPasswordReminder()
        {
            _logger.LogInformation("Bắt đầu gửi email nhắc nhở...");

            try
            {
                await _passwordReminderService.SendRemindersAsync();
                _logger.LogInformation("Gửi email nhắc nhở thành công.");

                return Ok(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status200OK,
                    "Send email reminder successfully!",
                    null
                ));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email nhắc nhở");
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi gửi email.", details = ex.Message });
            }
        }
    }
}
