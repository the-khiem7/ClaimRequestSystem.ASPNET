using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    public class StaffsController : BaseController<StaffsController>
    {
        private readonly IStaffService _staffService; // inject staff service vao staff controller

        public StaffsController(ILogger<StaffsController> logger, IStaffService staffService) : base(logger)
        {
            _staffService = staffService;
        }

        /// <summary>
        /// Get all active staff members
        /// </summary>
        [HttpGet(ApiEndPointConstant.Staffs.StaffsEndpoint)] 
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CreateStaffResponse>>), StatusCodes.Status200OK)] // tra ve response 200 OK
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)] // tra ve response 500 neu co loi
        public async Task<IActionResult> GetStaffs()
        {
            var staffs = await _staffService.GetStaffs();
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Staff list retrieved successfully",
                staffs
            ));
        }

        /// <summary>
        /// Get staff by Id
        /// </summary>
        [HttpGet(ApiEndPointConstant.Staffs.StaffEndpointById)]
        [ProducesResponseType(typeof(ApiResponse<CreateStaffResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStaffById(Guid id)
        {
            var staff = await _staffService.GetStaffById(id);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Staff retrieved successfully",
                staff
            ));
        }

        /// <summary>
        /// Update staff details
        /// </summary>
        [HttpPut(ApiEndPointConstant.Staffs.UpdateStaffEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<UpdateStaffResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffRequest request)
        {
            var updatedStaff = await _staffService.UpdateStaff(id, request);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Staff updated successfully",
                updatedStaff
            ));
        }

        /// <summary>
        /// Soft delete a staff member (set IsActive = false)
        /// </summary>
        [HttpDelete(ApiEndPointConstant.Staffs.DeleteStaffEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteStaff(Guid id)
        {
            await _staffService.DeleteStaff(id);
            return Ok(ApiResponseBuilder.BuildResponse<object>(
                StatusCodes.Status200OK,
                "Staff deleted successfully",
                null
            ));
        }

        /// <summary>
        /// Create a new staff member
        /// </summary>
        [HttpPost(ApiEndPointConstant.Staffs.StaffsEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<CreateStaffResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
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
    }
}
