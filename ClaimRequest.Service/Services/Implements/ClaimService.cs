﻿using System.Drawing;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Claim = ClaimRequest.DAL.Data.Entities.Claim;

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

        #region Nguyen_Anh_Quan
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
                        throw new InvalidOperationException("Claim cannot be cancelled as you are not the claimer.");
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

        public async Task<MemoryStream> DownloadClaimAsync(DownloadClaimRequest downloadClaimRequest)
        {
            try
            {
                if (_unitOfWork?.Context?.Database == null)
                {
                    throw new InvalidOperationException("Database context is not initialized.");
                }

                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;

                var selectedClaims = await _unitOfWork.GetRepository<Claim>().GetListAsync(
                    predicate: c => downloadClaimRequest.ClaimIds.Contains(c.Id),
                    include: c => c.Include(x => x.Claimer)
                        .Include(x => x.Project)
                        .Include(x => x.Finance)
                );

                if (selectedClaims == null || !selectedClaims.Any())
                {
                    _logger.LogWarning("No claims found for download.");
                    throw new NotFoundException("No claims found for download.");
                }

                // Ensure all selected claims have Status == Paid
                if (selectedClaims.Any(c => c.Status != ClaimStatus.Paid))
                {
                    _logger.LogWarning("Some claims have a status other than 'Paid'. Process aborted.");
                    throw new InvalidOperationException("All selected claims must have status 'Paid'.");
                }

                // Ensure all selected claims are updated in the current month and year
                if (selectedClaims.Any(c => c.UpdateAt == null || c.UpdateAt.Month != currentMonth || c.UpdateAt.Year != currentYear))
                {
                    _logger.LogWarning("Some claims are not from the current month. Process aborted.");
                    throw new InvalidOperationException("All selected claims must be updated in the current month.");
                }

                foreach (var claim in selectedClaims)
                {
                    List<string> missingFields = new List<string>();
                    if (claim.Claimer == null) missingFields.Add("Claimer");
                    if (claim.Project == null) missingFields.Add("Project");
                    if (claim.Finance == null) missingFields.Add("Finance");
                    if (string.IsNullOrEmpty(claim.Name)) missingFields.Add("Name");

                    if (missingFields.Any())
                    {
                        _logger.LogWarning($"Claim ID {claim.Id} has missing fields: {string.Join(", ", missingFields)}");
                        claim.Claimer ??= new Staff { Name = "Unknown Claimer" };
                        claim.Project ??= new Project { Name = "Unknown Project" };
                        claim.Finance ??= new Staff { Name = "Unknown Finance Approver" };
                    }
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Template Export Claim");

                // Define column headers
                var headers = new[] { "Claim ID", "Claimer Name", "Project Name", "Claim Type", "Status",
                      "Amount", "Total Working Hours", "Start Date", "End Date", "Created At",
                      "Finance Approver", "Remarks" };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.Green);
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Font.Bold = true;
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Populate data rows
                int row = 2;
                foreach (var claim in selectedClaims)
                {
                    worksheet.Cells[row, 1].Value = claim.Id;
                    worksheet.Cells[row, 2].Value = claim.Claimer?.Name;
                    worksheet.Cells[row, 3].Value = claim.Project?.Name;
                    worksheet.Cells[row, 4].Value = claim.ClaimType.ToString();
                    worksheet.Cells[row, 5].Value = claim.Status.ToString();
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "$#,##0.00";
                    worksheet.Cells[row, 6].Value = claim.Amount;
                    worksheet.Cells[row, 7].Value = claim.TotalWorkingHours;
                    worksheet.Cells[row, 8].Value = claim.StartDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 9].Value = claim.EndDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 10].Value = claim.CreateAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 11].Value = claim.Finance?.Name;
                    worksheet.Cells[row, 12].Value = claim.Remark ?? "N/A";
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Save the Excel file to a memory stream
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                _logger.LogInformation("Successfully generated claims report for {Count} claims.", selectedClaims.Count);

                return stream; // Return the file stream
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading claim: {Message}", ex.Message);
                throw;
            }
        }
        #endregion

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

        public async Task<UpdateClaimResponse> UpdateClaim(Guid Id, UpdateClaimRequest request)
        {
            try
            {
                var claimRepository = _unitOfWork.GetRepository<Claim>();
                var claim = await claimRepository.GetByIdAsync(Id);

                if (claim == null)
                {
                    return new UpdateClaimResponse
                    {
                        ClaimId = Id,
                        Message = "Claim not found",
                        Success = false
                    };
                }

                if (request.StartDate >= request.EndDate)
                {
                    return new UpdateClaimResponse
                    {
                        ClaimId = Id,
                        Message = "Start Date must be earlier than End Date",
                        Success = false
                    };
                }

                claim.StartDate = request.StartDate;
                claim.EndDate = request.EndDate;
                claim.TotalWorkingHours = request.TotalWorkingHours;
                claim.UpdateAt = DateTime.UtcNow;

                claimRepository.UpdateAsync(claim);
                await _unitOfWork.CommitAsync();

                return new UpdateClaimResponse
                {
                    ClaimId = claim.Id,
                    StartDate = claim.StartDate,
                    EndDate = claim.EndDate,
                    TotalWorkingHours = claim.TotalWorkingHours,
                    Success = true,
                    Message = "Claim updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim with ID {ClaimId}: {Message}", Id, ex.Message);
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
        public async Task<Claim> GetClaimById(Guid id)
        {
            try
            {
                var claimRepository = _unitOfWork.GetRepository<Claim>();
                var claim = (await claimRepository.SingleOrDefaultAsync(
                    c => new { c, c.Claimer, c.Project },
                    c => c.Id == id,
                    include: q => q.Include(c => c.Claimer).Include(c => c.Project)
                )).ValidateExists(id);

                return claim.c;

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

        public async Task<RejectClaimResponse> RejectClaim(Guid id, RejectClaimRequest rejectClaimRequest)
        {
            try
            {
                var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _unitOfWork.BeginTransactionAsync();
                    try
                    {
                        // Truy vấn claim từ Id và các dữ liệu liên quan cần thiết
                        var pendingClaim = await _unitOfWork.GetRepository<Claim>()
                            .SingleOrDefaultAsync(
                                predicate: s => s.Id == id,
                                include: q => q.Include(c => c.ClaimApprovers)
                            );

                        if (pendingClaim == null)
                        {
                            throw new KeyNotFoundException($"Claim with ID {id} not found.");
                        }

                        if (pendingClaim.Status != ClaimStatus.Pending)
                        {
                            throw new InvalidOperationException($"Claim with ID {id} is not in pending status.");
                        }

                        // Truy vấn approver của claim
                        var existingApprover = pendingClaim.ClaimApprovers
                            .FirstOrDefault(ca => ca.ApproverId == rejectClaimRequest.ApproverId);

                        // Chỉ xử lý nếu approver chưa tồn tại
                        if (existingApprover != null)
                        {
                            throw new BadRequestException($"Approver with ID {rejectClaimRequest.ApproverId} has already rejected this claim.");
                        }

                        var project = await _unitOfWork.GetRepository<Project>()
                    .SingleOrDefaultAsync(
                            predicate: s => s.Id == pendingClaim.ProjectId,
                            include: q => q.Include(p => p.ProjectManager)
                            );

                        if (project == null)
                        {
                            throw new NotFoundException($"Project for claim with ID {id} not found.");
                        }

                        // Kiểm tra xem Approver có phải Project Manager của dự án đó không
                        if (project.ProjectManagerId != rejectClaimRequest.ApproverId)
                        {
                            throw new UnauthorizedAccessException($"Approver with ID {rejectClaimRequest.ApproverId} does not have permission to reject this claim.");
                        }

                        var approverName = project?.ProjectManager?.Name ?? "Unknown Approver";

                        // Nếu claim chưa có approver thì tạo mới
                        var newApprover = new ClaimApprover
                        {
                            ClaimId = pendingClaim.Id,
                            ApproverId = rejectClaimRequest.ApproverId,
                        };
                        await _unitOfWork.GetRepository<ClaimApprover>().InsertAsync(newApprover);

                        // Cập nhật trạng thái claim
                        _mapper.Map(rejectClaimRequest, pendingClaim);

                        _unitOfWork.GetRepository<Claim>().UpdateAsync(pendingClaim);

                        // Chỉ ghi changelog khi approver lần đầu reject (Audit Trails)
                        var changeLog = new ClaimChangeLog
                        {
                            HistoryId = Guid.NewGuid(),
                            ClaimId = pendingClaim.Id,
                            FieldChanged = "Claim Status",
                            OldValue = "Pending",
                            NewValue = pendingClaim.Status.ToString(),
                            ChangedAt = DateTime.UtcNow,
                            ChangedBy = approverName
                        };
                        await _unitOfWork.GetRepository<ClaimChangeLog>().InsertAsync(changeLog);

                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        // Ánh xạ dữ liệu trả về
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
                _logger.LogError(ex, "Error rejecting claim with ID {id}: {Message} | StackTrace: {StackTrace}", id, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> ApproveClaim(Guid approverId, Guid id)
        {
            var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var claimRepo = _unitOfWork.GetRepository<Claim>();
                    var claimApproverRepo = _unitOfWork.GetRepository<ClaimApprover>();

                    var pendingClaim = (await claimRepo.SingleOrDefaultAsync(
                        predicate: s => s.Id == id,
                        include: s => s.Include(c => c.ClaimApprovers)
                    )).ValidateExists(id); ;


                    if (pendingClaim.Status != ClaimStatus.Pending)
                    {
                        throw new BadRequestException($"Claim with ID {id} is not in pending state.");
                    }

                    var isApproverAllowed = pendingClaim.ClaimApprovers
                        .Any(ca => ca.ApproverId == approverId);

                    if (!isApproverAllowed)
                    {
                        throw new UnauthorizedAccessException($"Approver with ID {approverId} does not have permission to this claim");
                    }

                    _logger.LogInformation("Approving claim with ID: {Id} by approver: {ApproveId}", id, approverId);
                    pendingClaim.Status = ClaimStatus.Approved;

                    claimRepo.UpdateAsync(pendingClaim);

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

        public async Task<ReturnClaimResponse> ReturnClaim(Guid id, ReturnClaimRequest returnClaimRequest)
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
                                predicate: s => s.Id == id,
                                include: q => q.Include(c => c.ClaimApprovers));

                        pendingClaim.ValidateExists(id);

                        if (pendingClaim.Status != ClaimStatus.Pending)
                        {
                            throw new BadRequestException($"Claim with ID {id} is not pending for approval.");
                        }

                        _logger.LogInformation("Returning claim with ID: {id} by approver: {Approverid}", id, returnClaimRequest.ApproverId);

                        var existingApprover = pendingClaim.ClaimApprovers
                            .FirstOrDefault(ca => ca.ApproverId == returnClaimRequest.ApproverId);

                        if (existingApprover == null)
                        {
                            throw new UnauthorizedAccessException($"Approver with ID {returnClaimRequest.ApproverId} does not have permission to return claim ID {id}.");
                        }

                        _mapper.Map(returnClaimRequest, pendingClaim);

                        pendingClaim.Status = ClaimStatus.Draft;
                        pendingClaim.UpdateAt = DateTime.UtcNow;

                        _unitOfWork.GetRepository<Claim>().UpdateAsync(pendingClaim);
                        await _unitOfWork.CommitAsync();
                        await transaction.CommitAsync();

                        return _mapper.Map<ReturnClaimResponse>(pendingClaim);
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
                _logger.LogError(ex, "Error returning claim with ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }


    }
}
