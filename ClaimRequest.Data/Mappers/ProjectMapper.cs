using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;
using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Mappers
{
    public class ProjectMapper : Profile
    {
        public ProjectMapper()
        {
            // CreateProjectRequest -> Project
            CreateMap<CreateProjectRequest, Project>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ProjectStatus.Draft))
                .ForMember(dest => dest.Claims, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectStaffs, opt => opt.Ignore());

            // Project -> CreateProjectResponse
            CreateMap<Project, CreateProjectResponse>()
                .ForMember(dest => dest.ProjectManager, opt => opt.MapFrom(src => src.ProjectManager));

            // UpdateProjectRequest -> Project
            CreateMap<UpdateProjectRequest, Project>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Claims, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectStaffs, opt => opt.Ignore());
        }
    }
}
