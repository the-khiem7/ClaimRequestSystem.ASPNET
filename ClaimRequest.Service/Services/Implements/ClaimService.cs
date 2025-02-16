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
using Microsoft.EntityFrameworkCore;

namespace ClaimRequest.BLL.Services.Implements
{
    public class ClaimService : BaseService<Claim>, IClaimService
    {
        public ClaimService(
            IUnitOfWork<ClaimRequestDbContext> unitOfWork,
            ILogger<Claim> logger,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest)
        {
            try
            {
                // Map request to entity
                var newClaim = _mapper.Map<Claim>(createClaimRequest);

                // Insert new claim
                await _unitOfWork.GetRepository<Claim>().InsertAsync(newClaim);

                // Save changes
                await _unitOfWork.CommitAsync();

                // Map and return response
                return _mapper.Map<CreateClaimResponse>(newClaim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim: {Message}", ex.Message);
                throw;
            }
        }
    }
}
