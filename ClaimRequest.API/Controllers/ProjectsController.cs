using ClaimRequest.API.Constants;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimRequest.API.Controllers
{
    [ApiController]
    [Authorize(Policy = "CanManageProjects")]
    public class ProjectsController : BaseController<ProjectsController>
    {
        private readonly IProjectService _projectService;

        public ProjectsController(ILogger<ProjectsController> logger, IProjectService projectService)
            : base(logger)
        {
            _projectService = projectService;
        }

        [HttpGet(ApiEndPointConstant.Projects.ProjectsEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<PagingResponse<CreateProjectResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectsPaginated(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string sortBy = "Name",
    [FromQuery] bool isDescending = false,
    [FromQuery] string? name = null,
    [FromQuery] string? description = null,
    [FromQuery] string? projectManagerName = null,
    [FromQuery] ProjectStatus? status = null,
    [FromQuery] Guid? projectManagerId = null,
    [FromQuery] ProjectRole? role = null,
    [FromQuery] decimal? minBudget = null,
    [FromQuery] decimal? maxBudget = null,
    [FromQuery] DateOnly? startDateFrom = null,
    [FromQuery] DateOnly? endDateTo = null,
    [FromQuery] bool? isActive = true
)
        {
            var paginatedProjects = await _projectService.GetProjects(
                page,
                pageSize,
                sortBy,
                isDescending,
                name,
                description,
                projectManagerName,
                status,
                projectManagerId,
                role,
                minBudget,
                maxBudget,
                startDateFrom,
                endDateTo,
                isActive
            );

            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Project list retrieved successfully",
                paginatedProjects
            ));
        }

        [HttpGet(ApiEndPointConstant.Projects.ProjectEndpointById)]
        [ProducesResponseType(typeof(ApiResponse<CreateProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectById(Guid id)
        {
            var project = await _projectService.GetProjectById(id);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Project retrieved successfully",
                project
            ));
        }

        [HttpPost(ApiEndPointConstant.Projects.ProjectsEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<CreateProjectResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            var response = await _projectService.CreateProject(request);

            if (response == null)
            {
                return BadRequest(
                    ApiResponseBuilder.BuildErrorResponse<object>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "Failed to create project",
                        "The project creation process failed"
                    )
                );
            }

            return CreatedAtAction(
                nameof(GetProjectById),
                new { id = response.Id },
                ApiResponseBuilder.BuildResponse(
                    StatusCodes.Status201Created,
                    "Project created successfully",
                    response
                )
            );
        }

        [HttpPut(ApiEndPointConstant.Projects.UpdateProjectEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<CreateProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
        {
            var updatedProject = await _projectService.UpdateProject(id, request);
            return Ok(ApiResponseBuilder.BuildResponse(
                StatusCodes.Status200OK,
                "Project updated successfully",
                updatedProject
            ));
        }

        [HttpPut(ApiEndPointConstant.Projects.DeleteProjectEndpoint)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            try
            {
                bool isDeleted = await _projectService.DeleteProject(id);
                if (!isDeleted)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = $"Project with ID {id} not found",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Project deleted successfully",
                    IsSuccess = true,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project: {Message}", ex.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "An unexpected error occurred while deleting the project",
                    Data = null
                });
            }
        }
    }
}
