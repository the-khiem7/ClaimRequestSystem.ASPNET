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
using ClaimRequest.DAL.Data.Exceptions;

namespace ClaimRequest.BLL.Services.Implements
{
    // chuẩn bị cho việc implement các method CRUD cho Staff 
    public class StaffService : BaseService<StaffService>, IStaffService
    {
        public StaffService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<StaffService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        // B3: Implement method CRUD cho Staff
        // nhớ tạo request và response DTO cho staff
        // method cho endpoint create staff
        public async Task<CreateStaffResponse> CreateStaff(CreateStaffRequest createStaffRequest)
        {
            try
            {
                // co the xay ra loi trong create, update, delete nen dung transaction
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
                        // su dung generic repository de insert staff => 
                        // ko can define tung repository cho tung entity
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

        // method cho endpoint get staff by id
        public async Task<CreateStaffResponse> GetStaffById(Guid id)
        {
            try
            {
                var staff = await _unitOfWork.GetRepository<Staff>()
                    .SingleOrDefaultAsync(
                        predicate: s => s.Id == id && s.IsActive,
                        include: q => q.Include(s => s.ProjectStaffs)
                    );

                if (staff == null)
                {
                    throw new NotFoundException($"Staff with ID {id} not found");
                }

                return _mapper.Map<CreateStaffResponse>(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff member: {Message}", ex.Message);
                throw;
            }
        }

        // method cho endpoint get all staffs
        public async Task<IEnumerable<CreateStaffResponse>> GetStaffs()
        {
            try
            {
                var staffs = await _unitOfWork.GetRepository<Staff>()
                    .GetListAsync(
                        predicate: s => s.IsActive,
                        include: q => q.Include(s => s.ProjectStaffs)
                    );

                return _mapper.Map<IEnumerable<CreateStaffResponse>>(staffs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff list: {Message}", ex.Message);
                throw;
            }
        }

        // method cho endpoint update staff
        // co the xay ra loi trong create, update, delete nen dung transaction
        // tao update request DTO cho staff (neu can)
        public async Task<UpdateStaffResponse> UpdateStaff(Guid id, UpdateStaffRequest updateStaffRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        var existingStaff = await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id && s.IsActive,
                                orderBy: null,
                                include: null
                            );

                        if (existingStaff == null)
                        {
                            throw new NotFoundException($"Staff with ID {id} not found");
                        }

                        // Update properties
                        _mapper.Map(updateStaffRequest, existingStaff);

                        _unitOfWork.GetRepository<Staff>().UpdateAsync(existingStaff);
                        await _unitOfWork.CommitAsync(); // save changes
                        await transaction.CommitAsync(); // thuc hien commit transaction => thay doi duoc luu vao db

                        return _mapper.Map<UpdateStaffResponse>(existingStaff);
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
                _logger.LogError(ex, "Error updating staff member: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<bool> DeleteStaff(Guid id)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        var staff = await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id && s.IsActive,
                                orderBy: null,
                                include: null
                            );

                        if (staff == null)
                        {
                            throw new NotFoundException($"Staff with ID {id} not found");
                        }

                        // Soft delete
                        staff.IsActive = false;
                        _unitOfWork.GetRepository<Staff>().UpdateAsync(staff);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return true;
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
                _logger.LogError(ex, "Error deleting staff member: {Message}", ex.Message);
                throw;
            }
        }


        //public static class PasswordHasher
        //{
        //    public static string HashPassword(string password)
        //    {
        //        using (SHA256 sha256 = SHA256.Create())
        //        {
        //            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        //        }
        //    }
        //}
    }
}
