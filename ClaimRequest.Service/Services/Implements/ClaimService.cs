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
        public ClaimService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<Claim> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
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

        public async Task<RejectClaimResponse> RejectClaim(Guid Id, RejectClaimRequest rejectClaimRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        var pendingClaim = await _unitOfWork.GetRepository<Claim>()
                        .SingleOrDefaultAsync(
                            predicate: s => s.Id == Id,
                            include: q => q.Include(c => c.ClaimApprovers)
                            );
                        if (pendingClaim == null)
                        {
                            throw new NotFoundException($"Claim with ID {Id} not found.");
                        }

                        if(pendingClaim.Status != ClaimStatus.Pending) 
                        {
                            throw new BadRequestException($"Claim with ID {Id} is not in pending.");
                        }
                        _logger.LogInformation("Rejecting claim with ID: {Id} by approver: {ApproverId}", Id, rejectClaimRequest.ApproverId);


                        var existingApprover = pendingClaim.ClaimApprovers
                            .FirstOrDefault(ca => ca.ApproverId == rejectClaimRequest.ApproverId);

                        if (existingApprover == null)
                        {
                            var newApprover = new ClaimApprover
                            {
                                ClaimId = pendingClaim.Id,
                                ApproverId = rejectClaimRequest.ApproverId
                            };

                            await _unitOfWork.GetRepository<ClaimApprover>().InsertAsync(newApprover);
                        }


                        _mapper.Map(rejectClaimRequest, pendingClaim);

                        pendingClaim.Status = ClaimStatus.Rejected;

                        _unitOfWork.GetRepository<Claim>().UpdateAsync(pendingClaim);
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<RejectClaimResponse>(pendingClaim);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim with ID {Id}: {Message}", Id, ex.Message);
                throw;
            }
        }


    }
}
