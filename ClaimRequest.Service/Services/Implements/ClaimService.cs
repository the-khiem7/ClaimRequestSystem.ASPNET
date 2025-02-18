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
using ClaimRequest.DAL.Data.Exceptions;

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
        public async Task<IEnumerable<ViewClaimResponse>> GetClaimsAsync(ClaimStatus? status)
        {
            if (status.HasValue && !Enum.IsDefined(typeof(ClaimStatus), status.Value))
            {
                throw new BadRequestException("Invalid claim status!");
            }

            var claimRepository = _unitOfWork.GetRepository<Claim>();
            var claims = await claimRepository.GetListAsync(
                c => new { c, c.Claimer, c.Project },
                c => !status.HasValue || c.Status == status.Value,
                include: q => q.Include(c => c.Claimer).Include(c => c.Project)
            );
            return _mapper.Map<IEnumerable<ViewClaimResponse>>(claims.Select(c => c.c));
        }

        public async Task<ViewClaimResponse> GetClaimByIdAsync(Guid id)
        {
            var claimRepository = _unitOfWork.GetRepository<Claim>();
            var claim = await claimRepository.SingleOrDefaultAsync(
                c => new { c, c.Claimer, c.Project },
                c => c.Id == id,
                include: q => q.Include(c => c.Claimer).Include(c => c.Project)
            );
            if (claim == null)
            {
                throw new NotFoundException($"Claim with ID {id} not found");
            }
            return _mapper.Map<ViewClaimResponse>(claim.c);
        }
    }
}
