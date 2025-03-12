using System.Drawing;
using System.Security.Claims;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.MetaDatas;
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

        private async Task LogChangeAsync(Guid claimId, string field, string oldValue, string newValue, string changedBy)
        {
            var changeLog = new ClaimChangeLog
            {
                HistoryId = Guid.NewGuid(),
                ClaimId = claimId,
                FieldChanged = field,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy
            };

            await _unitOfWork.GetRepository<ClaimChangeLog>().InsertAsync(changeLog);
        }


        #region Nguyen_Anh_Quan
        public async Task<CancelClaimResponse> CancelClaim(Guid claimId, CancelClaimRequest cancelClaimRequest)
        {
            try
            {
                if (_unitOfWork?.Context?.Database == null)
                {
                    throw new InvalidOperationException("Database context is not initialized.");
                }

                // Extract user ID from JWT
                var userId = _httpContextAccessor.HttpContext.User.FindFirst("StaffId")?.Value;
                if (userId == null)
                {
                    throw new UnauthorizedAccessException("User ID not found in JWT.");
                }

                // Get claim by ID first before starting the transaction
                var claim = await _unitOfWork.GetRepository<Claim>().GetByIdAsync(claimId)
                            ?? throw new KeyNotFoundException("Claim not found.");

                // Validate claim status and claimer BEFORE starting transaction
                if (claim.Status != ClaimStatus.Draft)
                {
                    throw new InvalidOperationException("Claim cannot be cancelled as it is not in Draft status.");
                }

                if (claim.ClaimerId.ToString() != userId)
                {
                    throw new InvalidOperationException("Claim cannot be cancelled as you are not the claimer.");
                }

                var result = await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    // Update claim status
                    claim.Status = ClaimStatus.Cancelled;
                    claim.UpdateAt = DateTime.UtcNow;
                    claim.Remark = cancelClaimRequest.Remark;

                    // Update claim
                    _unitOfWork.GetRepository<Claim>().UpdateAsync(claim);

                    // Log the change of claim status
                    _logger.LogInformation("Cancelled claim by {ClaimerId} on {Time}", userId, claim.UpdateAt);

                    await LogChangeAsync(claimId, "Claim Status", "Draft", ClaimStatus.Cancelled.ToString(), userId);

                    // Map and return response
                    return _mapper.Map<CancelClaimResponse>(claim);
                });

                return result;
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

                var foundClaimIds = selectedClaims.Select(c => c.Id).ToHashSet();
                var missingClaimIds = downloadClaimRequest.ClaimIds.Except(foundClaimIds).ToList();

                if (missingClaimIds.Any())
                {
                    _logger.LogWarning("The following claim IDs were not found: {MissingClaims}", string.Join(", ", missingClaimIds));
                    throw new NotFoundException($"Some claims were not found: {string.Join(", ", missingClaimIds)}");
                }

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
                    var cells = worksheet.Cells;

                    var values = new object[]
                    {
                        claim.Id,
                        claim.Claimer?.Name,
                        claim.Project?.Name,
                        claim.ClaimType.ToString(),
                        claim.Status.ToString(),
                        claim.Amount,
                        claim.TotalWorkingHours,
                        claim.StartDate.ToString("yyyy-MM-dd"),
                        claim.EndDate.ToString("yyyy-MM-dd"),
                        claim.CreateAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        claim.Finance?.Name,
                        claim.Remark ?? "N/A"
                    };

                    for (int col = 0; col < values.Length; col++)
                    {
                        var cell = cells[row, col + 1];
                        cell.Value = values[col];
                        if (col == 5) // Format Amount column
                            cell.Style.Numberformat.Format = "$#,##0.00";
                    }
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
                return await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    var newClaim = _mapper.Map<Claim>(createClaimRequest);
                    await _unitOfWork.GetRepository<Claim>().InsertAsync(newClaim);
                    return _mapper.Map<CreateClaimResponse>(newClaim);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<UpdateClaimResponse> UpdateClaim(Guid id, UpdateClaimRequest request)
        {
            try
            {
                return await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    var claimRepository = _unitOfWork.GetRepository<Claim>();
                    var claim = await claimRepository.GetByIdAsync(id);

                    if (claim == null)
                    {
                        throw new KeyNotFoundException("Claim not found");
                    }

                    if (request.StartDate >= request.EndDate)
                    {
                        _logger.LogError("Start Date {StartDate} must be earlier than End Date {EndDate}.", request.StartDate, request.EndDate);
                        throw new InvalidOperationException("Start Date must be earlier than End Date.");
                    }

                    claim.StartDate = request.StartDate;
                    claim.EndDate = request.EndDate;
                    claim.TotalWorkingHours = request.TotalWorkingHours;
                    claim.UpdateAt = DateTime.UtcNow;

                    claimRepository.UpdateAsync(claim);

                    _logger.LogInformation("Successfully updated claim with ID {ClaimId}.", id);
                    return _mapper.Map<UpdateClaimResponse>(claim);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim with ID {ClaimId}: {Message}", id, ex.Message);
                throw;
            }
        }


        public async Task<PagingResponse<ViewClaimResponse>> GetClaims(int pageNumber = 1, int pageSize = 20, ClaimStatus? status = null)
        {
            try
            {
                // Validate claim status
                if (status.HasValue && !Enum.IsDefined(typeof(ClaimStatus), status.Value))
                {
                    throw new BadRequestException("Invalid claim status!");
                }

                var response = await _unitOfWork.GetRepository<Claim>().GetPagingListAsync(
                    include: query => query.AsNoTracking()
                                           .Include(c => c.Project)
                                           .Include(c => c.Claimer),
                    predicate: c => !status.HasValue || c.Status == status.Value,
                    selector: c => _mapper.Map<ViewClaimResponse>(c),
                    page: pageNumber,
                    size: pageSize
                );

                return response;
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
                var claim = (await claimRepository.SingleOrDefaultAsync(
                    c => new { c, c.Claimer, c.Project },
                    c => c.Id == id,
                    include: q => q.Include(c => c.Claimer).Include(c => c.Project)
                )).ValidateExists(id);

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

        public async Task<RejectClaimResponse> RejectClaim(Guid id, RejectClaimRequest rejectClaimRequest)
        {
            try
            {
                return await _unitOfWork.ProcessInTransactionAsync(async () =>
                {
                    var pendingClaim = await _unitOfWork.GetRepository<Claim>()
                        .SingleOrDefaultAsync(
                            predicate: s => s.Id == id
                        ) ?? throw new KeyNotFoundException($"Claim with ID {id} not found.");

                    if (pendingClaim.Status != ClaimStatus.Pending)
                    {
                        throw new InvalidOperationException($"Claim with ID {id} is not in pending status.");
                    }

                    var projectStaff = await _unitOfWork.GetRepository<ProjectStaff>()
                        .SingleOrDefaultAsync(predicate: ps => ps.StaffId == rejectClaimRequest.ApproverId
                            && ps.ProjectId == pendingClaim.ProjectId);

                    if (projectStaff == null)
                    {
                        throw new UnauthorizedAccessException($"User with ID {rejectClaimRequest.ApproverId} is not in the right project to reject this claim.");
                    }

                    var approver = await _unitOfWork.GetRepository<Staff>()
                        .SingleOrDefaultAsync(predicate: s => s.Id == rejectClaimRequest.ApproverId)
                        ?? throw new KeyNotFoundException($"Approver with ID {id} not found.");

                    if (approver.SystemRole != SystemRole.Approver)
                    {
                        throw new UnauthorizedAccessException($"User with ID {rejectClaimRequest.ApproverId} does not have permission to reject this claim.");
                    }
                    var approverName = approver.Name ?? "Unknown Approver";

                    await _unitOfWork.GetRepository<ClaimApprover>().InsertAsync(new ClaimApprover
                    {
                        ClaimId = pendingClaim.Id,
                        ApproverId = rejectClaimRequest.ApproverId
                    });

                    _mapper.Map(rejectClaimRequest, pendingClaim);
                    _unitOfWork.GetRepository<Claim>().UpdateAsync(pendingClaim);

                    await LogChangeAsync(pendingClaim.Id, "Claim Status", "Pending", ClaimStatus.Rejected.ToString(), approverName);

                    return _mapper.Map<RejectClaimResponse>(pendingClaim);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim with ID {id}: {Message} | StackTrace: {StackTrace}", id, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<bool> ApproveClaim(ClaimsPrincipal user, Guid id)
        {
            var approverIdClaim = user.FindFirst("StaffId")?.Value;
            if (string.IsNullOrEmpty(approverIdClaim))
            {
                throw new UnauthorizedAccessException("Approver ID not found in token.");
            }

            var approverId = Guid.Parse(approverIdClaim);
            var executionStrategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                var claimRepo = _unitOfWork.GetRepository<Claim>();
                var claimApproverRepo = _unitOfWork.GetRepository<ClaimApprover>();

                var pendingClaim = await claimRepo.SingleOrDefaultAsync(
                    predicate: s => s.Id == id,
                    include: s => s.Include(c => c.ClaimApprovers)
                );

                if (pendingClaim == null)
                {
                    var claimRepo = _unitOfWork.GetRepository<Claim>();
                    var claimApproverRepo = _unitOfWork.GetRepository<ClaimApprover>();

                    var pendingClaim = (await claimRepo.SingleOrDefaultAsync(
                        predicate: s => s.Id == id,
                        include: s => s.Include(c => c.ClaimApprovers)
                    )).ValidateExists(id);

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

                if (pendingClaim.Status != ClaimStatus.Pending)
                {
                    throw new BadRequestException($"Claim with ID {id} is not in pending state.");
                }

                var isApproverAllowed = pendingClaim.ClaimApprovers
                    .Any(ca => ca.ApproverId == approverId);

                if (!isApproverAllowed)
                {
                    throw new UnauthorizedAccessException($"Approver with ID {approverId} does not have permission to approve this claim.");
                }

                _logger.LogInformation("Approving claim with ID: {Id} by approver: {ApproverId}", id, approverId);

                pendingClaim.Status = ClaimStatus.Approved;
                claimRepo.UpdateAsync(pendingClaim);

                return true;
            });
        }


        public async Task<ReturnClaimResponse> ReturnClaim(Guid id, ReturnClaimRequest returnClaimRequest)
        {
            try
            {
                return await _unitOfWork.ProcessInTransactionAsync(async () =>
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

                    return _mapper.Map<ReturnClaimResponse>(pendingClaim);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning claim with ID {Id}: {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task<bool> PaidClaim(Guid id, Guid financeId)
        {
            try
            {
                var existingClaim = (await _unitOfWork.GetRepository<Claim>()
                    .SingleOrDefaultAsync(predicate: c => c.Id == id).ValidateExists(id));


                // 🔹 Kiểm tra trạng thái của claim (chỉ được thanh toán khi Approved)
                if (existingClaim.Status != ClaimStatus.Approved)
                {
                    throw new BusinessException($"Cannot mark as Paid when the status is not Approved. Current Status: {existingClaim.Status}");
                }

                // 🔹 Kiểm tra Finance Staff có hợp lệ không
                var finance = await _unitOfWork.GetRepository<Staff>()
                    .SingleOrDefaultAsync(predicate: s => s.Id == financeId && s.SystemRole == SystemRole.Finance);

                if (finance == null)
                {
                    throw new BadRequestException($"Finance staff with ID {financeId} not found or does not have the Finance role.");
                }

                // 🔹 Cập nhật trạng thái của claim thành "Paid"
                var oldStatus = existingClaim.Status;
                existingClaim.Status = ClaimStatus.Paid;
                _unitOfWork.GetRepository<Claim>().UpdateAsync(existingClaim);
                await _unitOfWork.CommitAsync(); // Lưu thay đổi vào DB

                // 🔹 Ghi log thay đổi trạng thái
                var claimLog = new ClaimChangeLog
                {
                    HistoryId = Guid.NewGuid(),
                    ClaimId = existingClaim.Id,
                    FieldChanged = "Status",
                    OldValue = oldStatus.ToString(),
                    NewValue = ClaimStatus.Paid.ToString(),
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = finance.Name ?? "System"
                };

                await _unitOfWork.GetRepository<ClaimChangeLog>().InsertAsync(claimLog);
                await _unitOfWork.CommitAsync(); // Lưu log vào DB

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Paid Claim: {Message}", ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {InnerMessage}", ex.InnerException.Message);
                }

                throw;
            }
        }


    }
}
