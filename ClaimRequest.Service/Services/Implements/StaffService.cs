using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ClaimRequest.BLL.Services.Implements
{
    public class StaffService : BaseService<Staff>, IStaffService
    {
        public StaffService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<Staff> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<CreateStaffResponse> CreateStaff(CreateStaffRequest createStaffRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    // Begin transaction
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        // Map request to entity
                        var newStaff = _mapper.Map<Staff>(createStaffRequest);

                        // Insert new staff
                        await _unitOfWork.GetRepository<Staff>().InsertAsync(newStaff);

                        // Save changes
                        await _unitOfWork.CommitAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        // Map and return response
                        return _mapper.Map<CreateStaffResponse>(newStaff);
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
                _logger.LogError(ex, "Error creating staff member: {Message}", ex.Message);
                throw;
            }
        }
    }
}
