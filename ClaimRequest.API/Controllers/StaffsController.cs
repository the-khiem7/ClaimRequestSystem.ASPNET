using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Paging;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    [Authorize(Policy = "CanManageStaff")]
    public class StaffsController : BaseController<StaffsController>
    {
        private readonly IStaffService _staffService; // inject staff service vao staff controller

        public StaffsController(ILogger<StaffsController> logger, IStaffService staffService) : base(logger)
        {
            _staffService = staffService;
        }

        [HttpGet(ApiEndPointConstant.Staffs.StaffsEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CreateStaffResponse>>), StatusCodes.Status200OK)] // tra ve response 200 OK
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)] // tra ve response 500 neu co loi
        [Authorize(Roles = "Admin, Approver, Finance")]
        public async Task<IActionResult> GetStaffs()
        {
            var userClaims = User.Claims.Select(c => new {c.Type, c.Value}); //debugging code
            _logger.LogInformation("User claims: {@Claims}", userClaims);

            var staffs = await _staffService.GetStaffs();
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Staff list retrieved successfully",
                staffs
            ));
        }

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

        [HttpGet(ApiEndPointConstant.Staffs.StaffsEndpoint + "/paged")]
        [ProducesResponseType(typeof(ApiResponse<PagingResponse<CreateStaffResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPagedStaffs([FromQuery] PagingRequest pagingRequest)
        {
            var pagedStaffs = await _staffService.GetPagedStaffs(pagingRequest);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Paged staff list retrieved successfully",
                pagedStaffs
            ));
        }

        [HttpPut(ApiEndPointConstant.Staffs.UpdateStaffEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<UpdateStaffResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStaff(Guid id, [FromForm] UpdateStaffRequest request)
        {
            var updatedStaff = await _staffService.UpdateStaff(id, request);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Staff updated successfully",
                updatedStaff
            ));
        }

        [HttpPut(ApiEndPointConstant.Staffs.DeleteStaffEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStaff(Guid id)
        {
            await _staffService.DeleteStaff(id);
            return Ok(ApiResponseBuilder.BuildResponse<object>(
                StatusCodes.Status200OK,
                "Staff deleted successfully",
                null
            ));
        }

        [HttpPost(ApiEndPointConstant.Staffs.StaffsEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<CreateStaffResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff([FromForm] CreateStaffRequest request)
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

        [HttpPost(ApiEndPointConstant.Staffs.AssignStaffEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<AssignStaffResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignStaff(Guid id ,[FromBody] AssignStaffRequest request)
        {
            var response = await _staffService.AssignStaff(id ,request);

            if (response == null)
            {
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "Failed to assign staff",
                        "The staff assigning process failed"
                    )
                );
            }

            return CreatedAtAction(
                nameof(GetStaffById),
                new { id = response.Id },
                ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status201Created,
                    "Staff assigned to project successfully",
                    response
                )
            );
        }

        [HttpDelete(ApiEndPointConstant.Staffs.RemoveStaffEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<RemoveStaffResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveStaff(Guid id, [FromBody] RemoveStaffRequest request)
        {
            var response = await _staffService.RemoveStaff(id, request);
            return Ok(ApiResponseBuilder.BuildResponse<object>(
                StatusCodes.Status200OK,
                "Staff removed successfully",
                response
            ));
        }

    }
}
