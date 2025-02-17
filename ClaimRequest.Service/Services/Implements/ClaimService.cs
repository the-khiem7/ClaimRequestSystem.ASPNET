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
                //Đảm bảo tính toàn vẹn của dự liệu khi thực hiện transaction
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    // Nếu có lỗi thì sẽ rollback lại
                    await using var transaction = await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        // Tìm Claim với điều kiện yêu cầu
                        var pendingClaim = await _unitOfWork.GetRepository<Claim>()
                        .SingleOrDefaultAsync(
                            predicate: s => s.Id == Id && s.Status == ClaimStatus.Pending
                            );
                        if (pendingClaim == null)
                        {
                            throw new NotFoundException($"Claim with ID {Id} not found or it is not in 'Pending' status.");
                        }
                        // Ghi lại log ai đã từ chối claim nào, phục vụ cho audit trail (sau này).
                        _logger.LogInformation("Rejecting claim with ID: {Id} by approver: {ApproverId}", Id, rejectClaimRequest.ApproverId);

                        // Cập nhật vào Claim
                        _mapper.Map(rejectClaimRequest, pendingClaim);

                        // Set status về rejected
                        pendingClaim.Status = ClaimStatus.Rejected;

                        // Lưu vào db
                        _unitOfWork.GetRepository<Claim>().UpdateAsync(pendingClaim);
                        await _unitOfWork.CommitAsync(); // save changes
                        await transaction.CommitAsync(); // thuc hien commit transaction => thay doi duoc luu vao db

                        return _mapper.Map<RejectClaimResponse>(pendingClaim);
                    }
                    // Rollback nếu c ó lỗi
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            } 
            // Bắt lỗi gì đó không lường trước :v
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff member: {Message}", ex.Message);
                throw;
            }
        }


    }
}
