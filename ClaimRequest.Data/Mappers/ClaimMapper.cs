using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;

namespace ClaimRequest.DAL.Mappers
{
    class ClaimMapper : Profile
    {
        public ClaimMapper()
        {
            CreateMap<CreateClaimRequest, Claim>();
            CreateMap<Claim, CreateClaimResponse>();
        }
    }
}
