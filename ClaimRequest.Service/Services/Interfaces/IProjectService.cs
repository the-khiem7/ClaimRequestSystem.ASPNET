using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IProjectService
    {
        // B2: Tao method CRUD cho Project
        Task<CreateProjectResponse> CreateProject(CreateProjectRequest createProjectRequest);
        Task<CreateProjectResponse> GetProjectById(Guid id);
        Task<IEnumerable<CreateProjectResponse>> GetProjects();
        Task<CreateProjectResponse> UpdateProject(Guid id, UpdateProjectRequest updateProjectRequest);
        Task<bool> DeleteProject(Guid id);
    }
}
