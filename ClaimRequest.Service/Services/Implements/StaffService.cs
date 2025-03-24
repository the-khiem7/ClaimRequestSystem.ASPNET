﻿using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Staff;
using ClaimRequest.DAL.Data.Responses.Staff;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClaimRequest.DAL.Data.Requests.Paging;
using ClaimRequest.DAL.Data.Responses.Paging;

namespace ClaimRequest.BLL.Services.Implements
{
    // chuẩn bị cho việc implement các method CRUD cho Staff 
    public class StaffService : BaseService<StaffService>, IStaffService
    {
        private readonly IConfiguration _configuration;
        private readonly ICloudinaryService _cloudinaryService;
        private const string DefaultProfilePicture = "https://static.vecteezy.com/system/resources/previews/009/292/244/non_2x/default-avatar-icon-of-social-media-user-vector.jpg";
        public StaffService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, ILogger<StaffService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ICloudinaryService cloudinaryService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _configuration = configuration;
            _cloudinaryService = cloudinaryService;
            _httpContextAccessor = httpContextAccessor;
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
                //// Sử dụng ExecutionStrategy để retry nếu có lỗi tạm thời trong DB
                //var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                //return await executionStrategy.ExecuteAsync(async () =>
                //{
                //    // Bắt đầu transaction
                //    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                //    try
                //    {
                //        Expression<Func<Staff, bool>> predicate = s => s.Email == createStaffRequest.Email;
                //        var existingStaff = await _unitOfWork.GetRepository<Staff>()
                //            .SingleOrDefaultAsync(predicate: s => s.Email == createStaffRequest.Email);

                //        if (existingStaff != null)
                //        {
                //            throw new BusinessException("Email is already in use. Please use a different email.");
                //        }

                //        // Ánh xạ từ Request sang Entity
                //        var newStaff = _mapper.Map<Staff>(createStaffRequest);
                //        newStaff.Id = Guid.NewGuid(); // Tạo ID mới
                //        newStaff.Password = BCrypt.Net.BCrypt.HashPassword(createStaffRequest.Password); // Hash mật khẩu

                //        // Thêm vào DB
                //        await _unitOfWork.GetRepository<Staff>().InsertAsync(newStaff);
                //        await _unitOfWork.CommitAsync();

                //        // Commit transaction trước khi trả về response
                //        await transaction.CommitAsync();
                //        return _mapper.Map<CreateStaffResponse>(newStaff);
                //    }
                //    catch (Exception ex)
                //    {
                //        await transaction.RollbackAsync();
                //        Console.WriteLine($"Error creating staff: {ex.Message}");
                //        throw;
                //    }
                //});

                return await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    var existingStaff = await _unitOfWork.GetRepository<Staff>()
                        .SingleOrDefaultAsync(predicate: s => s.Email == createStaffRequest.Email);

                    if (existingStaff != null)
                    {
                        throw new BusinessException("Email is already in use. Please use a different email.");
                    }

                    var newStaff = _mapper.Map<Staff>(createStaffRequest);
                    newStaff.Id = Guid.NewGuid();
                    newStaff.Password = BCrypt.Net.BCrypt.HashPassword(createStaffRequest.Password);
                    var user = _httpContextAccessor.HttpContext?.User;

                    if (createStaffRequest.Avatar != null && user != null)
                    {
                        newStaff.Avatar = await _cloudinaryService.UploadImageAsync(createStaffRequest.Avatar, user);
                    }
                    else
                    {
                        newStaff.Avatar = DefaultProfilePicture;
                    }

                    await _unitOfWork.GetRepository<Staff>().InsertAsync(newStaff);
                    await _unitOfWork.CommitAsync();

                    return _mapper.Map<CreateStaffResponse>(newStaff);
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
                return await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    var existingStaff = (await _unitOfWork.GetRepository<Staff>()
                        .SingleOrDefaultAsync(
                            predicate: s => s.Id == id && s.IsActive,
                            orderBy: null,
                            include: null
                        )).ValidateExists(id, "Can't update because this staff");
                    string currentAvatar = existingStaff.Avatar;
                    _mapper.Map(updateStaffRequest, existingStaff);
                    var user = _httpContextAccessor.HttpContext?.User;
                    if (updateStaffRequest.Avatar != null && user != null)
                    {
                        existingStaff.Avatar = await _cloudinaryService.UploadImageAsync(updateStaffRequest.Avatar, user);
                    }
                    else
                    {
                        existingStaff.Avatar = currentAvatar;
                    }
                    _unitOfWork.GetRepository<Staff>().UpdateAsync(existingStaff);
                    await _unitOfWork.CommitAsync();

                    return _mapper.Map<UpdateStaffResponse>(existingStaff);
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
                return await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    var staff = (await _unitOfWork.GetRepository<Staff>()
                        .SingleOrDefaultAsync(
                            predicate: s => s.Id == id && s.IsActive,
                            orderBy: null,
                            include: null
                        )).ValidateExists(id, "Can't delete because this staff");

                    staff.IsActive = false;
                    _unitOfWork.GetRepository<Staff>().UpdateAsync(staff);

                    await _unitOfWork.CommitAsync();

                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff member: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<AssignStaffResponse> AssignStaff(Guid id ,AssignStaffRequest assignStaffRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        // Validate staff if exists
                        var existingStaff = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id && s.IsActive
                            )).ValidateExists(id);

                        // Validate if project exists
                        var existingProject = await _unitOfWork.GetRepository<Project>()
                            .SingleOrDefaultAsync(
                            predicate: p => p.Id == assignStaffRequest.projectId
                            ).ValidateExists(assignStaffRequest.projectId);

                        // Check valid inputted role
                        if (!Enum.IsDefined(typeof(ProjectRole), assignStaffRequest.ProjectRole))
                        {
                            throw new BadRequestException("Invalid project role.");
                        }

                        // Validate if assigner is in project
                        var assignerInProject = await _unitOfWork.GetRepository<ProjectStaff>()
                            .SingleOrDefaultAsync(predicate: ps => ps.StaffId == assignStaffRequest.AssignerId
                                                 && ps.ProjectId == assignStaffRequest.projectId);

                        if (assignerInProject == null)
                        {
                            throw new BadRequestException("You are not a member of this project.");
                        }

                        // Ensure assigner is project manager
                        if (assignerInProject.ProjectRole != ProjectRole.ProjectManager)
                        {
                            throw new UnauthorizedException("You do not have permission to assign staff to this project.");
                        }

                        // Check if staff is assigned to the project
                        var projectStaff = await _unitOfWork.GetRepository<ProjectStaff>()
                            .SingleOrDefaultAsync(predicate: s => s.StaffId == id
                            && s.ProjectId == assignStaffRequest.projectId);

                        if (projectStaff != null)
                        {
                            throw new BadRequestException("Staff already assign to this project.");
                        }

                        // Assign staff to project
                        var newProjectStaff = _mapper.Map<ProjectStaff>(assignStaffRequest);
                        newProjectStaff.Id = Guid.NewGuid();
                        newProjectStaff.StaffId = id;

                        await _unitOfWork.GetRepository<ProjectStaff>().InsertAsync(newProjectStaff);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<AssignStaffResponse>(newProjectStaff);
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
                _logger.LogError(ex, "Error Assigning staff member: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<RemoveStaffResponse> RemoveStaff(Guid id, RemoveStaffRequest removeStaffRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
                    try
                    {
                        // Validate staff if exists
                        var existingStaff = (await _unitOfWork.GetRepository<Staff>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id && s.IsActive
                            )).ValidateExists(id);

                        // Validate project if exists
                        var existingProject = await _unitOfWork.GetRepository<Project>()
                            .SingleOrDefaultAsync(
                            predicate: p => p.Id == removeStaffRequest.projectId
                            ).ValidateExists(removeStaffRequest.projectId);

                        // Remover must be project manager
                        var removerInProject = await _unitOfWork.GetRepository<ProjectStaff>()
                            .SingleOrDefaultAsync(predicate: ps => ps.StaffId == removeStaffRequest.RemoverId
                                                 && ps.ProjectId == removeStaffRequest.projectId
                                                 && ps.ProjectRole == ProjectRole.ProjectManager);

                        if (removerInProject == null)
                        {
                            throw new BadRequestException("You are not a member of this project or not project manager.");
                        }

                        // Check if staff is assigned to the project
                        var projectStaff = await _unitOfWork.GetRepository<ProjectStaff>()
                            .SingleOrDefaultAsync(predicate: s => s.StaffId == id
                            && s.ProjectId == removeStaffRequest.projectId);

                        if (projectStaff == null)
                        {
                            throw new BadRequestException("Staff is not assign to this project.");
                        }

                        // Cannot remove project manager
                        if (projectStaff.ProjectRole == ProjectRole.ProjectManager) 
                        {
                            throw new BadRequestException("Project Manager cannot be remove from project.");
                        }

                        _unitOfWork.GetRepository<ProjectStaff>().DeleteAsync(projectStaff);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<RemoveStaffResponse>(projectStaff);
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
                _logger.LogError(ex, "Error Assigning staff member: {Message}", ex.Message);
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

        public async Task<PagingResponse<CreateStaffResponse>> GetPagedStaffs(PagingRequest pagingRequest)
        {
            var pagedStaffs = await _unitOfWork.GetRepository<Staff>()
                .GetPagingListAsync(
                    predicate: null,
                    orderBy: q => q.OrderBy(s => s.Name),
                    include: null,
                    page: pagingRequest.Page,
                    size: pagingRequest.PageSize
                );

            var response = new PagingResponse<CreateStaffResponse>
            {
                Items = _mapper.Map<IEnumerable<CreateStaffResponse>>(pagedStaffs.Items),
                Meta = new PaginationMeta
                {
                    TotalPages = pagedStaffs.Meta.TotalPages,
                    TotalItems = pagedStaffs.Meta.TotalItems,
                    CurrentPage = pagedStaffs.Meta.CurrentPage,
                    PageSize = pagedStaffs.Meta.PageSize
                }
            };

            return response;
        }
    }
}
