using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffsController : BaseController<StaffsController>
    {
        private readonly IStaffService _staffService;

        public StaffsController(ILogger<StaffsController> logger, IStaffService staffService) : base(logger)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public IActionResult GetStaffs()
        {
            return Ok();
        }
        [HttpGet]
        [Route("{id}")]
        public IActionResult GetStaffById(string id)
        {
            return Ok();
        }
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CreateStaffResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            try
            {
                var response = await _staffService.CreateStaff(request);

                if (response == null)
                {
                    return BadRequest(
                        ApiResponseBuilder.BuildErrorResponse<object>(
                            null,
                            StatusCodes.Status400BadRequest,
                            "Failed to create staff",
                            "The staff creation process failed"
                        )
                    );
                }

                return CreatedAtAction(
                    nameof(GetStaffById),
                    new { id = response.Id },
                    ApiResponseBuilder.BuildResponse(
                        StatusCodes.Status201Created,
                        "Staff created successfully",
                        response
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff: {Message}", ex.Message);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status500InternalServerError,
                        "An error occurred while creating the staff",
                        ex.Message
                    )
                );
            }
        }
        [HttpPut]
        public IActionResult UpdateStaff()
        {
            return Ok();
        }
    }
}
