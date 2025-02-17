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

        public async Task<CancelClaimResponse> CancelClaim(CancelClaimRequest cancelClaimRequest)
        {
            try
            {
                if (_unitOfWork?.Context?.Database == null)
                {
                    throw new InvalidOperationException("Database context is not initialized.");
                }

                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    // Get claim by ID first before starting the transaction
                    var claim = await _unitOfWork.GetRepository<Claim>().GetByIdAsync(cancelClaimRequest.ClaimId)
                                ?? throw new KeyNotFoundException("Claim not found.");

                    // Validate claim status and claimer BEFORE starting transaction
                    if (claim.Status != ClaimStatus.Draft)
                    {
                        throw new InvalidOperationException("Claim cannot be cancelled as it is not in Draft status.");
                    }

                    if (claim.ClaimerId != cancelClaimRequest.ClaimerId)
                    {
                        throw new UnauthorizedAccessException("Claim cannot be cancelled as you are not the claimer.");
                    }

                    // Begin transaction only after all validation checks have passed
                    await using var transaction = await _unitOfWork.BeginTransactionAsync();

                    try
                    {
                        // Update claim status
                        claim.Status = ClaimStatus.Cancelled;
                        claim.UpdateAt = DateTime.UtcNow;

                        // Update claim
                        _unitOfWork.GetRepository<Claim>().UpdateAsync(claim);

                        // Commit changes and transaction
                        await _unitOfWork.CommitAsync();
                        await _unitOfWork.CommitTransactionAsync(transaction);

                        // Log the change of claim status
                        _logger.LogInformation("Cancelled claim by {ClaimerId} on {Time}", cancelClaimRequest.ClaimerId, claim.UpdateAt);

                        // Map and return response
                        return _mapper.Map<CancelClaimResponse>(claim);
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction only if transaction has started
                        await _unitOfWork.RollbackTransactionAsync(transaction);
                        _logger.LogError(ex, "Error occurred during claim cancellation.");
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
