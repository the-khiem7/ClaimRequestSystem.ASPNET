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
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.Claimer, opt => opt.Ignore())
                .ForMember(dest => dest.ClaimApprovers, opt => opt.Ignore())
                .ForMember(dest => dest.ChangeHistory, opt => opt.Ignore());

            // Map Claim to CreateClaimResponse
            CreateMap<Claim, CreateClaimResponse>();
        }
    }
}
