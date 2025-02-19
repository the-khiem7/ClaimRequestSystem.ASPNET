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
        public async Task<IEnumerable<ViewClaimResponse>> GetClaims(ClaimStatus? status)
        {
            try
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
            catch (BadRequestException)
            {
                throw; // Rethrow known exception
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving claims.");
                throw;
            }
        }

        public async Task<ViewClaimResponse> GetClaimById(Guid id)
        {
            try
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
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the claim.");
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


        public async Task<ApproveClaimResponse> ApproveClaim(Guid Id, ApproveClaimRequest approveClaimRequest)
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

                        if (pendingClaim.Status != ClaimStatus.Pending)
                        {
                            throw new BadRequestException($"Claim with ID {Id} is not in pending.");
                        }
                        _logger.LogInformation("Approving claim with ID: {Id} by approver: {ApproverId}", Id, approveClaimRequest.ApproverId);


                        var existingApprover = pendingClaim.ClaimApprovers
                            .FirstOrDefault(ca => ca.ApproverId == approveClaimRequest.ApproverId);

                        if (existingApprover == null)
                        {
                            var newApprover = new ClaimApprover
                            {
                                ClaimId = pendingClaim.Id,
                                ApproverId = approveClaimRequest.ApproverId
                            };

                            await _unitOfWork.GetRepository<ClaimApprover>().InsertAsync(newApprover);
                        }


                        _mapper.Map(approveClaimRequest, pendingClaim);

                        pendingClaim.Status = ClaimStatus.Rejected;

                        _unitOfWork.GetRepository<Claim>().UpdateAsync(pendingClaim);
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<ApproveClaimResponse>(pendingClaim);
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
                _logger.LogError(ex, "Error approve claim with ID {Id}: {Message}", Id, ex.Message);
                throw;
            }
        }


    }
}
