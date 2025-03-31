using AutoMapper;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Project;
using ClaimRequest.DAL.Data.Responses.Project;

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
                .ForMember(dest => dest.ProjectManager, opt => opt.MapFrom(src => src.ProjectManager))
                .ForMember(dest => dest.FinanceStaff, opt => opt.MapFrom(src => src.FinanceStaff))
                .ForMember(dest => dest.ProjectStaffs, opt => opt.MapFrom(src => src.ProjectStaffs)); // Include project staff

            // UpdateProjectRequest -> Project
            CreateMap<UpdateProjectRequest, Project>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Claims, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectStaffs, opt => opt.Ignore());

            // ProjectStaff -> ProjectStaffResponse (Include Staff and Department)
            CreateMap<ProjectStaff, ProjectStaffResponse>()
                .ForMember(dest => dest.StaffId, opt => opt.MapFrom(src => src.Staff.Id))
                .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff.Name))
                .ForMember(dest => dest.StaffEmail, opt => opt.MapFrom(src => src.Staff.Email))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Staff.Department)) // Map Department
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.ProjectRole));
        }
    }
}