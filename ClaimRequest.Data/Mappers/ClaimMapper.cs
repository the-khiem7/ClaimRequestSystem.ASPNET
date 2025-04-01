using AutoMapper;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using Claim = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.DAL.Mappers
{
    public class ClaimMapper : Profile
    {
        public ClaimMapper()
        {
            CreateMap<CreateClaimRequest, Claim>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ClaimStatus.Draft))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Finance, opt => opt.Ignore())
                .ForMember(dest => dest.FinanceId, opt => opt.Ignore())
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.Claimer, opt => opt.Ignore())
                .ForMember(dest => dest.ClaimApprovers, opt => opt.Ignore())
                .ForMember(dest => dest.ChangeHistory, opt => opt.Ignore());

            // Map Claim to CreateClaimResponse
            CreateMap<Claim, CreateClaimResponse>();

            //CreateMap<Claim, CreateClaimResponse>()
            //    .ForMember(dest => dest.Project, opt => opt.MapFrom(src => src.Project));

            // UpdateClaimRequest -> Claim
            CreateMap<UpdateClaimRequest, Claim>()
                .ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.TotalWorkingHours, opt => opt.MapFrom(src => src.TotalWorkingHours))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            // Claim -> UpdateClaimResponse
            CreateMap<Claim, UpdateClaimResponse>()
                .ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.TotalWorkingHours, opt => opt.MapFrom(src => src.TotalWorkingHours))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt));
            // CancelClaimRequest -> Claim
            CreateMap<CancelClaimRequest, Claim>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ClaimStatus.Cancelled))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            // Claim -> CancelClaimResponse
            CreateMap<Claim, CancelClaimResponse>()
                .ForMember(dest => dest.ClaimId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Cancelled"))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt))
                .ForMember(dest => dest.ClaimerId, opt => opt.MapFrom(src => src.ClaimerId));

            CreateMap<Claim, ViewClaimResponse>()
                .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Claimer.Name))
                .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name))
                .ForMember(dest => dest.ProjectStartDate, opt => opt.MapFrom(src => src.Project.StartDate))
                .ForMember(dest => dest.ProjectEndDate, opt => opt.MapFrom(src => src.Project.EndDate));

            // Add this mapping to the ClaimMapper class
            CreateMap<Claim, ViewClaimByIdResponse>()
                // Claim details
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.TotalWorkingHours, opt => opt.MapFrom(src => src.TotalWorkingHours))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.ClaimType, opt => opt.MapFrom(src => src.ClaimType))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreateAt))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt))

                // Nested objects
                .ForMember(dest => dest.Claimer, opt => opt.MapFrom(src => src.Claimer))
                .ForMember(dest => dest.Project, opt => opt.MapFrom(src => src.Project))
                .ForMember(dest => dest.ProjectManager, opt => opt.MapFrom(src => src.Project.ProjectManager))
                .ForMember(dest => dest.Finance, opt => opt.MapFrom(src => src.Finance));

            // Add mappings for the nested classes
            CreateMap<Staff, ViewClaimByIdResponse.StaffDetails>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.SystemRole, opt => opt.MapFrom(src => src.SystemRole))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.Salary, opt => opt.MapFrom(src => src.Salary))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.LastChangePassword, opt => opt.MapFrom(src => src.LastChangePassword));

            CreateMap<Project, ViewClaimByIdResponse.ProjectDetails>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.Budget, opt => opt.MapFrom(src => src.Budget))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));


            // Add missing mappings
            CreateMap<ReturnClaimRequest, Claim>()
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Claim, ReturnClaimResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ApproverId, opt => opt.MapFrom(src => src.ClaimApprovers.FirstOrDefault().ApproverId))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt));

            CreateMap<Claim, SubmitClaimRequest>()
                .ForMember(dest => dest.ClaimerId, opt => opt.MapFrom(src => src.Claimer.Id));

            CreateMap<Claim, SubmitClaimResponse>()
                .ForMember(dest => dest.ClaimerId, opt => opt.MapFrom(src => src.Claimer.Id));
                


        }
    }
}
