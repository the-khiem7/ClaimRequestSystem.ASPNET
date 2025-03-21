using AutoMapper;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;

namespace ClaimRequest.DAL.Mappers
{
    // tao automapper profile de map tu Request Model -> Entity -> Response Model
    public class StaffMapper : Profile
    {
        public StaffMapper()
        {
            // CreateStaffRequest -> Staff
            // nhung field nao khong co trong request model
            // xem xet 1: ignore (ko map) hoac 2: set gia tri mac dinh 
            // 3: theo SRS => suy dien
            CreateMap<CreateStaffRequest, Staff>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.ProjectStaffs, opt => opt.Ignore());

            // Staff -> CreateStaffResponse
            // xem xet nhung field Response can tra ve cho client
            // Define mapping cho nhung thuoc tinh ko cung ten (Name -> ResponseName) nhung cung muc dich
            CreateMap<Staff, CreateStaffResponse>()
                .ForMember(dest => dest.SystemRole, opt => opt.MapFrom(src => src.SystemRole))
                .ForMember(dest => dest.ResponseName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar));

            // UpdateStaffRequest -> Staff
            // update request model co the khac voi create request model
            // tao rieng mapping cho update request model
            CreateMap<UpdateStaffRequest, Staff>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectStaffs, opt => opt.Ignore())
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar));

            // Staff -> UpdateStaffResponse
            // update response model co the khac voi create response model
            CreateMap<Staff, UpdateStaffResponse>();


            // ProjectStaff --> AssignStaffResponse
            CreateMap<AssignStaffRequest, ProjectStaff>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // AssignStaffRequest --> ProjectStaff
            CreateMap<ProjectStaff, AssignStaffResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

            // ProjectStaff --> RemoveStaffResponse
            CreateMap<RemoveStaffRequest, ProjectStaff>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // RemoveStaffRequest --> ProjectStaff
            CreateMap<ProjectStaff, RemoveStaffResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));
        }
    }
}