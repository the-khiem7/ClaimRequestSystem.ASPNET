using AutoMapper;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;

namespace ClaimRequest.DAL.Mappers
{
    public class RejectClaimMapper : Profile
    {
        public RejectClaimMapper()
        {
            CreateMap<RejectClaimRequest, Claim>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ClaimStatus.Rejected))
                .ForMember(dest => dest.UpdateAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Claim, RejectClaimResponse>()
                .ForMember(dest => dest.ApproverId, opt => opt.MapFrom(src => src.ClaimApprovers.FirstOrDefault().ApproverId));
        }
    }
}
