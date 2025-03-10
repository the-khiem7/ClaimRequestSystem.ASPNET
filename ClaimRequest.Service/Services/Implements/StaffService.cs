using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using ClaimRequest.DAL.Data.Exceptions;

namespace ClaimRequest.BLL.Services.Implements
{
    // chuẩn bị cho việc implement các method CRUD cho Staff 
    public class StaffService : BaseService<StaffService>, IStaffService
    {

        private readonly IConfiguration _configuration;
        public StaffService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<StaffService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _configuration = configuration;
        }

        // B3: Implement method CRUD cho Staff
        // nhớ tạo request và response DTO cho staff
        // method cho endpoint create staff
        public async Task<CreateStaffResponse> CreateStaff(CreateStaffRequest createStaffRequest)
        {
            try
            {
                if (createStaffRequest == null)
                {
                    throw new ArgumentNullException(nameof(createStaffRequest), "Request data cannot be null.");
                }

                // Sử dụng ExecutionStrategy để retry nếu có lỗi tạm thời trong DB
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    // Bắt đầu transaction
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        Expression<Func<Staff, bool>> predicate = s => s.Email == createStaffRequest.Email;
                        var existingStaff = await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(predicate: s => s.Email == createStaffRequest.Email);

                        if (existingStaff != null)
                        {
                            throw new BusinessException("Email is already in use. Please use a different email.");
                        }

                        // Ánh xạ từ Request sang Entity
                        var newStaff = _mapper.Map<Staff>(createStaffRequest);
                        newStaff.Id = Guid.NewGuid(); // Tạo ID mới
                        newStaff.Password = BCrypt.Net.BCrypt.HashPassword(createStaffRequest.Password); // Hash mật khẩu

                        // Thêm vào DB
                        await _unitOfWork.GetRepository<Staff>().InsertAsync(newStaff);
                        await _unitOfWork.CommitAsync();

                        // Commit transaction trước khi trả về response
                        await transaction.CommitAsync();
                        return _mapper.Map<CreateStaffResponse>(newStaff);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Error creating staff: {ex.Message}");
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
                var staff = (await _unitOfWork.GetRepository<Staff>()
                    .SingleOrDefaultAsync(
                        predicate: s => s.Id == id && s.IsActive,
                        include: q => q.Include(s => s.ProjectStaffs)
                    )).ValidateExists(id, "Can't find because this staff");

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
                        var existingStaff = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id && s.IsActive,
                                orderBy: null,
                                include: null
                            )).ValidateExists(id, "Can't update because this staff");

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
                        var staff = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id && s.IsActive,
                                orderBy: null,
                                include: null
                            )).ValidateExists(id, "Can't delete because this staff");

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


        public static class PasswordHasher
        {
            public static string HashPassword(string password)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                }
            }
        }
    }
}
