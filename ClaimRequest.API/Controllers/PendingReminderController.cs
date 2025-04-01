using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.MetaDatas;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [Route(ApiEndPointConstant.PendingReminder.PendingReminderEndpoint)]
    [ApiController]
    public class PendingReminderController : ControllerBase
    {
        private readonly IPendingReminderService _pendingReminderService;
        private readonly ILogger<PendingReminderController> _logger;
        public PendingReminderController(IPendingReminderService pendingReminderService, ILogger<PendingReminderController> logger)
        {
            _pendingReminderService = pendingReminderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendPendingReminder()
        {
            _logger.LogInformation("Bắt đầu gửi email nhắc nhở...");

            try
            {
                await _pendingReminderService.SendReminderAsync();
                _logger.LogInformation("Gửi email nhắc nhở thành công.");

                return Ok(ApiResponseBuilder.BuildResponse<object>(
                    StatusCodes.Status200OK,
                    "Send email pending reminder successfully!",
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
