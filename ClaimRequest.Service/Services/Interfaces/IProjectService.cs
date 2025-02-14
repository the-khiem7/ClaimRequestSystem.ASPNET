using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
