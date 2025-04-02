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
                        if (createProjectRequest.EndDate.HasValue &&
                            createProjectRequest.StartDate > createProjectRequest.EndDate.Value)
                        {
                            throw new ArgumentException("StartDate must be earlier than or equal to EndDate.");
                        }

                        // Validate Project Manager
                        var projectManager = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == createProjectRequest.ProjectManagerId
                            )).ValidateExists(createProjectRequest.ProjectManagerId, "Can't create project because of invalid project manager");

                        if (projectManager.SystemRole != SystemRole.Admin &&
                            projectManager.SystemRole != SystemRole.Approver)
                        {
                            throw new InvalidOperationException("The specified staff member can't be Project Manager");
                        }

                        if (!projectManager.IsActive)
                        {
                            throw new InvalidOperationException("The specified Project Manager is not active");
                        }

                        // Validate Finance Staff
                        var financeStaff = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == createProjectRequest.FinanceStaffId
                            )).ValidateExists(createProjectRequest.FinanceStaffId, "Can't create project because of invalid finance staff");

                        if (financeStaff.SystemRole != SystemRole.Finance ||
                            financeStaff.Department != Department.FinancialDivision)
                        {
                            throw new InvalidOperationException("The specified staff member can't be Finance staff");
                        }

                        if (!financeStaff.IsActive)
                        {
                            throw new InvalidOperationException("The specified Finance Staff is not active");
                        }

                        // Create new Project entity
                        var newProject = _mapper.Map<Project>(createProjectRequest);
                        newProject.Status = createProjectRequest.Status;

                        await _unitOfWork.GetRepository<Project>().InsertAsync(newProject);
                        await _unitOfWork.CommitAsync();

                        // Add Project Manager to ProjectStaffs
                        var projectManagerStaff = new ProjectStaff
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = newProject.Id,
                            StaffId = createProjectRequest.ProjectManagerId,
                            ProjectRole = ProjectRole.ProjectManager
                        };
                        await _unitOfWork.GetRepository<ProjectStaff>().InsertAsync(projectManagerStaff);

                        // Add Finance Staff to ProjectStaffs
                        var financeProjectStaff = new ProjectStaff
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = newProject.Id,
                            StaffId = createProjectRequest.FinanceStaffId,
                            ProjectRole = ProjectRole.Finance
                        };
                        await _unitOfWork.GetRepository<ProjectStaff>().InsertAsync(financeProjectStaff);

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
                            .Include(p => p.FinanceStaff)
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
                    Guid? financeStaffId = null,
                    string? financeStaffName = null,
                    ProjectRole? role = null,
                    decimal? minBudget = null,
                    decimal? maxBudget = null,
                    DateOnly? startDateFrom = null,
                    DateOnly? endDateTo = null,
                    bool? isActive = true,
                    Guid? staffId = null
                )
        {
            try
            {
                // First, build the predicate for the initial filtering
                // Get the repository
                var repository = _unitOfWork.GetRepository<Project>();

                // Create a query expression for the initial filter
                Expression<Func<Project, bool>> predicate = p => isActive == null || p.IsActive == isActive.Value;

                // Get the initial dataset with includes
                var initialDataset = await repository.GetListAsync(
                    predicate: predicate,
                    include: q => q
                        .Include(p => p.ProjectManager)
                        .Include(p => p.FinanceStaff)
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

                if (staffId.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p =>
                        p.ProjectStaffs.Any(ps => ps.StaffId == staffId.Value));
                }

                if (financeStaffId.HasValue)
                {
                    filteredProjects = filteredProjects.Where(p => p.FinanceStaffId == financeStaffId.Value);
                }

                if (!string.IsNullOrEmpty(financeStaffName))
                {
                    filteredProjects = filteredProjects.Where(p => p.FinanceStaff != null &&
                        p.FinanceStaff.Name.Contains(financeStaffName, StringComparison.OrdinalIgnoreCase));
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
                        var projectRepo = _unitOfWork.GetRepository<Project>();
                        var staffRepo = _unitOfWork.GetRepository<Staff>();
                        var projectStaffRepo = _unitOfWork.GetRepository<ProjectStaff>();

                        // Load existing project with related data
                        var existingProject = await projectRepo.SingleOrDefaultAsync(
                            predicate: p => p.Id == id,
                            include: q => q.Include(p => p.ProjectStaffs)
                        );

                        existingProject.ValidateExists(id, "Project not found.");

                        // Validate date
                        if (updateProjectRequest.EndDate.HasValue && updateProjectRequest.StartDate > updateProjectRequest.EndDate.Value)
                            throw new ArgumentException("StartDate must be earlier than or equal to EndDate.");

                        // Update basic project fields
                        existingProject.Name = updateProjectRequest.Name;
                        existingProject.Description = updateProjectRequest.Description;
                        existingProject.StartDate = updateProjectRequest.StartDate;
                        existingProject.EndDate = updateProjectRequest.EndDate;
                        existingProject.Budget = updateProjectRequest.Budget;
                        existingProject.Status = updateProjectRequest.Status.Value;

                        // Update Project Manager
                        if (updateProjectRequest.ProjectManagerId != existingProject.ProjectManagerId)
                        {
                            var newPM = await staffRepo.SingleOrDefaultAsync(
                                predicate: s => s.Id == updateProjectRequest.ProjectManagerId
                            );

                            if ((newPM.SystemRole != SystemRole.Admin && newPM.SystemRole != SystemRole.Approver && newPM.SystemRole != SystemRole.Staff) || !newPM.IsActive)
                                throw new InvalidOperationException("Invalid or inactive Project Manager.");

                            existingProject.ProjectManagerId = updateProjectRequest.ProjectManagerId;

                            // Update or add ProjectStaff record for ProjectManager
                            var existingPMStaff = existingProject.ProjectStaffs.FirstOrDefault(ps => ps.ProjectRole == ProjectRole.ProjectManager);
                            if (existingPMStaff != null)
                            {
                                existingPMStaff.StaffId = updateProjectRequest.ProjectManagerId;
                                projectStaffRepo.UpdateAsync(existingPMStaff);
                            }
                            else
                            {
                                var pmStaff = new ProjectStaff
                                {
                                    Id = Guid.NewGuid(),
                                    ProjectId = existingProject.Id,
                                    StaffId = updateProjectRequest.ProjectManagerId,
                                    ProjectRole = ProjectRole.ProjectManager
                                };
                                await projectStaffRepo.InsertAsync(pmStaff);
                            }
                        }

                        // Update Finance Staff
                        if (updateProjectRequest.FinanceStaffId != existingProject.FinanceStaffId)
                        {
                            var newFinanceStaff = await staffRepo.SingleOrDefaultAsync(
                                predicate: s => s.Id == updateProjectRequest.FinanceStaffId
                            );

                            if (newFinanceStaff.SystemRole != SystemRole.Finance || newFinanceStaff.Department != Department.FinancialDivision || !newFinanceStaff.IsActive)
                                throw new InvalidOperationException("Invalid or inactive Finance Staff.");

                            existingProject.FinanceStaffId = updateProjectRequest.FinanceStaffId;

                            // Update or add ProjectStaff record for Finance
                            var existingFinanceStaff = existingProject.ProjectStaffs.FirstOrDefault(ps => ps.ProjectRole == ProjectRole.Finance);
                            if (existingFinanceStaff != null)
                            {
                                existingFinanceStaff.StaffId = updateProjectRequest.FinanceStaffId;
                                projectStaffRepo.UpdateAsync(existingFinanceStaff);
                            }
                            else
                            {
                                var financeStaff = new ProjectStaff
                                {
                                    Id = Guid.NewGuid(),
                                    ProjectId = existingProject.Id,
                                    StaffId = updateProjectRequest.FinanceStaffId,
                                    ProjectRole = ProjectRole.Finance
                                };
                                await projectStaffRepo.InsertAsync(financeStaff);
                            }
                        }

                        // Explicitly save changes
                        projectRepo.UpdateAsync(existingProject);
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        // Re-fetch updated project including all relations for accurate response mapping
                        var updatedProject = await projectRepo.SingleOrDefaultAsync(
    predicate: p => p.Id == existingProject.Id,
    include: q => q.Include(p => p.ProjectManager)
                   .Include(p => p.FinanceStaff)
                   .Include(p => p.ProjectStaffs)
                       .ThenInclude(ps => ps.Staff)
);

                        return _mapper.Map<CreateProjectResponse>(updatedProject);
                    }
                    catch
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
