using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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
                .ForMember(dest => dest.ClaimApprovers, opt => opt.Ignore())
                .ForMember(dest => dest.ChangeHistory, opt => opt.Ignore());

            //CreateMap<Project, ProjectResponse>();

            //CreateMap<Claim, CreateClaimResponse>()
            //    .ForMember(dest => dest.Project, opt => opt.MapFrom(src => src.Project));


            //CancelClaimRequest -> Claim
            CreateMap<CancelClaimRequest, Claim>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ClaimStatus.Cancelled))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
            //Claim -> CancelClaimResponse
            CreateMap<Claim, CancelClaimResponse>()
                .ForMember(dest => dest.ClaimId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Cancelled"))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(src => src.UpdateAt))
                .ForMember(dest => dest.ClaimerId, opt => opt.MapFrom(src => src.ClaimerId));
        }
    }
}
