using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Claim = ClaimRequest.DAL.Data.Entities.Claim;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;

namespace ClaimRequest.BLL.Services.Implements
{
    public class ClaimService : BaseService<Claim>, IClaimService
    {
        public ClaimService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<Claim> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest)
        {

            //MAP CAC GIA TRI TRONG OBJECT CLAIM TU FRONTEND VAO ENTITY CLAIM
            Claim newClaim = _mapper.Map<Claim>(createClaimRequest);
            
            
            await _unitOfWork.GetRepository<Claim>().InsertAsync(newClaim);
            bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

            //Ctor
            CreateClaimResponse createClaimResponse = new CreateClaimResponse();

            if(isSuccessful)
            {
                createClaimResponse = _mapper.Map<CreateClaimResponse>(newClaim);
            }
            return createClaimResponse;

        }
    }
}
