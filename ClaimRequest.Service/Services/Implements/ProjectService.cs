using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


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

                        if (projectManager.SystemRole != SystemRole.ProjectManager)
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
                        // Retrieve the project by ID
                        var existingProject = await _unitOfWork.GetRepository<Project>()
                            .SingleOrDefaultAsync(predicate: p => p.Id == id);

                        if (existingProject == null)
                        {
                            return false; // Return false instead of throwing an exception
                        }

                        // Delete the project
                        _unitOfWork.GetRepository<Project>().DeleteAsync(existingProject);
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
                        predicate: p => p.Id == id,
                        include: q => q
                            .Include(p => p.ProjectManager)
                            .Include(p => p.ProjectStaffs)
                    )).ValidateExists(id);

                return _mapper.Map<CreateProjectResponse>(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<CreateProjectResponse>> GetProjects()
        {
            try
            {
                var projects = await _unitOfWork.GetRepository<Project>()
                    .GetListAsync(
                        include: q => q
                            .Include(p => p.ProjectManager)
                            .Include(p => p.ProjectStaffs)
                    );

                return _mapper.Map<IEnumerable<CreateProjectResponse>>(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project list: {Message}", ex.Message);
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
                            )).ValidateExists(id, "Can't update becase this project ");

                        _mapper.Map(updateProjectRequest, existingProject);

                        // If a new Project Manager is provided, update it
                        if (updateProjectRequest.ProjectManagerId != Guid.Empty)
                        {
                            var newProjectManager = await _unitOfWork.GetRepository<Staff>()
                                .SingleOrDefaultAsync(
                                    predicate: s => s.Id == updateProjectRequest.ProjectManagerId,
                                    orderBy: null,
                                    include: null
                                ).ValidateExists(updateProjectRequest.ProjectManagerId, "Can't update becase this project because your ProjectManager ");

                            if (newProjectManager.SystemRole != SystemRole.ProjectManager)
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
