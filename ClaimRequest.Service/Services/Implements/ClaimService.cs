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
        public ClaimService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<Claim> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CancelClaimResponse> CancelClaim(CancelClaimRequest cancelClaimRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    // Begin transaction
                    await using var transaction = await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        // Get claim by id
                        var claim = await _unitOfWork.GetRepository<Claim>().GetByIdAsync(cancelClaimRequest.ClaimId);
                        if (claim == null)
                        {
                            throw new Exception("Claim not found.");
                        }
                        if (claim.Status != ClaimStatus.Draft)
                        {
                            throw new Exception("Claim cannot be cancelled as it is not in Draft status.");
                        }
                        if (claim.ClaimerId != cancelClaimRequest.ClaimerId)
                        {
                            throw new Exception("Claim cannot be cancelled as you are not the claimer.");
                        }
                        // Update claim status
                        claim.Status = ClaimStatus.Cancelled;
                        claim.UpdateAt = DateTime.UtcNow;
                        // Update claim
                        _unitOfWork.GetRepository<Claim>().UpdateAsync(claim);
                        // Save changes
                        await _unitOfWork.CommitAsync();
                        // Commit transaction
                        await _unitOfWork.CommitTransactionAsync(transaction);
                        // Map and return response
                        return _mapper.Map<CancelClaimResponse>(claim);
                    }
                    catch (Exception)
                    {
                        await _unitOfWork.RollbackTransactionAsync(transaction);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling claim: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    // Begin transaction
                    await using var transaction = await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        // Map request to entity
                        var newClaim = _mapper.Map<Claim>(createClaimRequest);

                        // Insert new claim
                        await _unitOfWork.GetRepository<Claim>().InsertAsync(newClaim);

                        // Save changes
                        await _unitOfWork.CommitAsync();

                        // Commit transaction
                        await _unitOfWork.CommitTransactionAsync(transaction);

                        // Map and return response
                        return _mapper.Map<CreateClaimResponse>(newClaim);
                    }
                    catch (Exception)
                    {
                        await _unitOfWork.RollbackTransactionAsync(transaction);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim: {Message}", ex.Message);
                throw;
            }
        }

    }
}
