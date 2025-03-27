using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;


namespace ClaimRequest.BLL.Services.Implements
{
    public class ProjectService : BaseService<Project>, IProjectService
    {
        public ProjectService(IUnitOfWork<ClaimRequestDbContext> unitOfWork,
             ILogger<Project> logger,
             IMapper mapper,
             IHttpContextAccessor httpContextAccessor)
             : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CreateProjectResponse> CreateProject(CreateProjectRequest createProjectRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        // Verify Project Manager exists and is valid
                        var projectManager = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == createProjectRequest.ProjectManagerId,
                                orderBy: null,
                                include: null
                            )).ValidateExists(createProjectRequest.ProjectManagerId, "Can't create project because of invalid project manager");

                        if (projectManager.SystemRole != SystemRole.Approver)// Add Admin
                        {
                            throw new InvalidOperationException("The specified staff member is not a Project Manager");
                        }

                        if (!projectManager.IsActive)
                        {
                            throw new InvalidOperationException("The specified Project Manager is not active");
                        }

                        var newProject = _mapper.Map<Project>(createProjectRequest);
                        newProject.Status = createProjectRequest.Status;  // Set status here

                        await _unitOfWork.GetRepository<Project>().InsertAsync(newProject);
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<CreateProjectResponse>(newProject);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteProject(Guid id)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        
                        var existingProject = await _unitOfWork.GetRepository<Project>()
                            .SingleOrDefaultAsync(predicate: p => p.Id == id);

                        if (existingProject == null)
                        {
                            return false; // Return false instead of throwing an exception
                        }

                        
                        existingProject.IsActive = false;
                        _unitOfWork.GetRepository<Project>().UpdateAsync(existingProject);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateProjectResponse> GetProjectById(Guid id)
        {
            try
            {
                var project = (await _unitOfWork.GetRepository<Project>()
                    .SingleOrDefaultAsync(
                        predicate: p => p.Id == id && p.IsActive,
                        include: q => q
                            .Include(p => p.ProjectManager)
                            .Include(p => p.ProjectStaffs)
                                .ThenInclude(ps => ps.Staff) // Load Staff, including Department
                    )).ValidateExists(id);

                return _mapper.Map<CreateProjectResponse>(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<PagingResponse<CreateProjectResponse>> GetProjects(
    int page,
    int pageSize,
    string sortBy = "Name",
    bool isDescending = false,
    string? name = null,
    string? description = null,
    string? projectManagerName = null,
    ProjectStatus? status = null,
    Guid? projectManagerId = null,
    ProjectRole? role = null,
    decimal? minBudget = null,
    decimal? maxBudget = null,
    DateOnly? startDateFrom = null,
    DateOnly? endDateTo = null,
    bool? isActive = true
)
        {
            try
            {
                // First, build the predicate for the initial filtering
                // We'll use a combination of the available methods in your repository

                // Get the repository
                var repository = _unitOfWork.GetRepository<Project>();

                // Create a query expression for the initial filter
                Expression<Func<Project, bool>> predicate = p => isActive == null || p.IsActive == isActive.Value;

                // Get the initial dataset with includes
                var initialDataset = await repository.GetListAsync(
                    predicate: predicate,
                    include: q => q
                        .Include(p => p.ProjectManager)
                        .Include(p => p.ProjectStaffs)
                            .ThenInclude(ps => ps.Staff)
                );

                // Now we have the initial dataset, apply additional filters in memory
                var filteredProjects = initialDataset.AsQueryable();

                // Apply Partial Match Filtering
                if (!string.IsNullOrEmpty(name))
                {
                    filteredProjects = filteredProjects.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(description))
                {
                    filteredProjects = filteredProjects.Where(p => p.Description.Contains(description, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(projectManagerName))
                {
                    filteredProjects = filteredProjects.Where(p => p.ProjectManager != null &&
                        p.ProjectManager.Name.Contains(projectManagerName, StringComparison.OrdinalIgnoreCase));
                }

                // Apply Filtering for Other Fields
                if (status.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.Status == status.Value);
                }

                if (projectManagerId.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.ProjectManagerId == projectManagerId.Value);
                }

                if (role.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.ProjectStaffs.Any(ps => ps.ProjectRole == role.Value));
                }

                if (minBudget.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.Budget >= minBudget.Value);
                }

                if (maxBudget.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.Budget <= maxBudget.Value);
                }

                if (startDateFrom.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.StartDate >= startDateFrom.Value);
                }

                if (endDateTo.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.EndDate <= endDateTo.Value);
                }

                // Apply Sorting
                filteredProjects = sortBy switch
                {
                    "Id" => isDescending ? filteredProjects.OrderByDescending(p => p.Id) : filteredProjects.OrderBy(p => p.Id),
                    "Name" => isDescending ? filteredProjects.OrderByDescending(p => p.Name) : filteredProjects.OrderBy(p => p.Name),
                    "Description" => isDescending ? filteredProjects.OrderByDescending(p => p.Description) : filteredProjects.OrderBy(p => p.Description),
                    "StartDate" => isDescending ? filteredProjects.OrderByDescending(p => p.StartDate) : filteredProjects.OrderBy(p => p.StartDate),
                    "EndDate" => isDescending ? filteredProjects.OrderByDescending(p => p.EndDate) : filteredProjects.OrderBy(p => p.EndDate),
                    "Budget" => isDescending ? filteredProjects.OrderByDescending(p => p.Budget) : filteredProjects.OrderBy(p => p.Budget),
                    "Status" => isDescending ? filteredProjects.OrderByDescending(p => p.Status) : filteredProjects.OrderBy(p => p.Status),
                    "ProjectManager" => isDescending
                        ? filteredProjects.OrderByDescending(p => p.ProjectManager != null ? p.ProjectManager.Name : string.Empty)
                        : filteredProjects.OrderBy(p => p.ProjectManager != null ? p.ProjectManager.Name : string.Empty),
                    _ => filteredProjects.OrderBy(p => p.Name) // Default sorting by name
                };

                // Manual pagination
                var totalItems = filteredProjects.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Apply pagination
                var paginatedProjects = filteredProjects
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to DTOs
                var mappedItems = _mapper.Map<IEnumerable<CreateProjectResponse>>(paginatedProjects);

                // Create pagination metadata
                var paginationMeta = new PaginationMeta
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                };

                // Create and return the final response
                return new PagingResponse<CreateProjectResponse>
                {
                    Items = mappedItems,
                    Meta = paginationMeta
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated project list: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateProjectResponse> UpdateProject(Guid id, UpdateProjectRequest updateProjectRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        var existingProject = (await _unitOfWork.GetRepository<Project>()
                            .SingleOrDefaultAsync(
                                predicate: p => p.Id == id,
                                include: q => q
                                    .Include(p => p.ProjectManager)
                                    .Include(p => p.ProjectStaffs)
                            )).ValidateExists(id, "Can't update because this project doesn't exist ");

                        _mapper.Map(updateProjectRequest, existingProject);


                        // If a new Project Manager is provided, update it
                        if (updateProjectRequest.ProjectManagerId != Guid.Empty)
                        {
                            var newProjectManager = await _unitOfWork.GetRepository<Staff>()
                                .SingleOrDefaultAsync(
                                    predicate: s => s.Id == updateProjectRequest.ProjectManagerId,
                                    orderBy: null,
                                    include: null
                                ).ValidateExists(updateProjectRequest.ProjectManagerId, "Can't update this project because your ProjectManager doesn't exist ");

                            if (newProjectManager.SystemRole != SystemRole.Approver)
                            {
                                throw new InvalidOperationException("The specified staff member is not a Project Manager");
                            }

                            if (!newProjectManager.IsActive)
                            {
                                throw new InvalidOperationException("The specified Project Manager is not active");
                            }

                            // Assign the new Project Manager
                            existingProject.ProjectManagerId = updateProjectRequest.ProjectManagerId;
                            existingProject.ProjectManager = newProjectManager;
                        }

                        // Only update the status if provided
                        if (updateProjectRequest.Status.HasValue)
                        {
                            existingProject.Status = updateProjectRequest.Status.Value;
                        }

                        _unitOfWork.GetRepository<Project>().UpdateAsync(existingProject);
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<CreateProjectResponse>(existingProject);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project: {Message}", ex.Message);
                throw;
            }
        }

    }
}
