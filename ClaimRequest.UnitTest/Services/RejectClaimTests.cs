using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;
using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.UnitTest.Services
{
    public class RejectClaimTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<ClaimEntity>> _mockClaimRepository;
        private readonly Mock<IGenericRepository<ProjectStaff>> _mockProjectStaffRepository;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly ClaimService _claimService;

        public RejectClaimTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();
            _mockProjectStaffRepository = new Mock<IGenericRepository<ProjectStaff>>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimEntity>()).Returns(_mockClaimRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<ProjectStaff>()).Returns(_mockProjectStaffRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>()).Returns(_mockStaffRepository.Object);

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<RejectClaimResponse>>>()))
                .Returns<Func<Task<RejectClaimResponse>>>(operation => operation());

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        public void Dispose() => GC.SuppressFinalize(this);

        [Fact]
        public async Task RejectClaim_ShouldReturn_RejectClaimResponse_WhenSuccessful()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Rejected due to insufficient documentation"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ProjectId = projectId
            };

            var projectStaff = new ProjectStaff
            {
                StaffId = approverId,
                ProjectId = projectId
            };

            var approver = new Staff
            {
                Id = approverId,
                SystemRole = SystemRole.Approver,
                Name = "John Approver"
            };

            var expectedResponse = new RejectClaimResponse
            {
                Id = claimId,
                Status = ClaimStatus.Rejected,
                Remark = rejectClaimRequest.Remark
            };

            // Setup repository mocks with correct method signature
            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockProjectStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(projectStaff);

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(approver);

            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>())).Verifiable();

            _mockMapper.Setup(m => m.Map(rejectClaimRequest, claim)).Verifiable();
            _mockMapper.Setup(m => m.Map<RejectClaimResponse>(It.IsAny<ClaimEntity>())).Returns(expectedResponse);

            // Setup mock for ClaimChangeLog repository
            var mockClaimChangeLogRepository = new Mock<IGenericRepository<ClaimChangeLog>>();
            mockClaimChangeLogRepository.Setup(repo => repo.InsertAsync(It.IsAny<ClaimChangeLog>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimChangeLog>())
                .Returns(mockClaimChangeLogRepository.Object);
            // Act
            var result = await _claimService.RejectClaim(claimId, rejectClaimRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(expectedResponse.Status, result.Status);
            Assert.Equal(expectedResponse.Remark, result.Remark);

            _mockClaimRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()), Times.Once);
            _mockMapper.Verify(m => m.Map(rejectClaimRequest, claim), Times.Once);
        }

        [Fact]
        public async Task RejectClaim_ShouldThrowException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to reject non-existing claim"
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync((ClaimEntity)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _claimService.RejectClaim(claimId, rejectClaimRequest));

            Assert.Equal($"Claim with ID {claimId} not found.", exception.Message);
        }

        [Fact]
        public async Task RejectClaim_ShouldThrowException_WhenClaimNotInPendingStatus()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to reject a claim not in pending status"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Draft // Not pending
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _claimService.RejectClaim(claimId, rejectClaimRequest));

            Assert.Equal($"Claim with ID {claimId} is not in pending status.", exception.Message);
        }

        [Fact]
        public async Task RejectClaim_ShouldThrowException_WhenApproverNotInProject()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to reject by approver not in project"
            };

            // Setup pending claim
            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ProjectId = projectId
            };

            // Setup existing approver
            var approver = new Staff
            {
                Id = approverId,
                IsActive = true
            };

            // Setup claim repository to return a pending claim
            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            // Setup staff repository to return the approver
            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(approver);

            // Setup project staff repository to return null (approver not in project)
            _mockProjectStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync((ProjectStaff)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _claimService.RejectClaim(claimId, rejectClaimRequest));

            Assert.Equal($"User with ID {approverId} is not in the right project to reject this claim.", exception.Message);
        }


        [Fact]
        public async Task RejectClaim_ShouldThrowException_WhenApproverNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to reject with non-existing approver"
            };

            // Setup pending claim
            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ProjectId = projectId
            };

            // Setup claim repository to return a pending claim
            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            // Setup staff repository to return null (approver not found)
            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync((Staff)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _claimService.RejectClaim(claimId, rejectClaimRequest));

            Assert.Equal($"Approver with ID {rejectClaimRequest.ApproverId} not found.", exception.Message);
        }

        [Fact]
        public async Task RejectClaim_ShouldThrowException_WhenUserNotApprover()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to reject by non-approver"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ProjectId = projectId
            };

            var projectStaff = new ProjectStaff
            {
                StaffId = approverId,
                ProjectId = projectId
            };

            var staff = new Staff
            {
                Id = approverId,
                SystemRole = SystemRole.Staff, // Not an approver
                Name = "John Staff"
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockProjectStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(projectStaff);

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staff);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _claimService.RejectClaim(claimId, rejectClaimRequest));

            Assert.Equal($"User with ID {approverId} does not have permission to reject this claim.", exception.Message);
        }

        [Fact]
        public async Task RejectClaim_ShouldThrowException_WhenApproverIdNotProvided()
        {
            // Arrange
            var claimId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = Guid.Empty,
                Remark = "Missing approver ID"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _claimService.RejectClaim(claimId, rejectClaimRequest));

            Assert.Equal($"Approver with ID {Guid.Empty} not found.", exception.Message);
        }

        // Test Audit trails (Changelog)
        [Fact]
        public async Task RejectClaim_ShouldCallLogChangeAsync_WhenSuccessful()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var rejectClaimRequest = new RejectClaimRequest
            {
                ApproverId = approverId,
                Remark = "Rejected due to insufficient documentation"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ProjectId = projectId
            };

            var projectStaff = new ProjectStaff
            {
                StaffId = approverId,
                ProjectId = projectId
            };

            var approver = new Staff
            {
                Id = approverId,
                SystemRole = SystemRole.Approver,
                Name = "John Approver"
            };

            var expectedResponse = new RejectClaimResponse
            {
                Id = claimId,
                Status = ClaimStatus.Rejected,
                Remark = rejectClaimRequest.Remark
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockProjectStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ProjectStaff, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IOrderedQueryable<ProjectStaff>>>(),
                It.IsAny<Func<IQueryable<ProjectStaff>, IIncludableQueryable<ProjectStaff, object>>>()))
                .ReturnsAsync(projectStaff);

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(approver);

            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>())).Verifiable();

            _mockMapper.Setup(m => m.Map(rejectClaimRequest, claim)).Verifiable();
            _mockMapper.Setup(m => m.Map<RejectClaimResponse>(It.IsAny<ClaimEntity>())).Returns(expectedResponse);

            // Set up mock for LogChangeAsync verification
            var logChangeAsyncCalled = false;
            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimChangeLog>().InsertAsync(It.IsAny<ClaimChangeLog>()))
                .Callback<ClaimChangeLog>(log => 
                {
                    Assert.Equal(claimId, log.ClaimId);
                    Assert.Equal("Claim Status", log.FieldChanged);
                    Assert.Equal(ClaimStatus.Pending.ToString(), log.OldValue);
                    Assert.Equal(ClaimStatus.Rejected.ToString(), log.NewValue);
                    Assert.Equal(approver.Name, log.ChangedBy);
                    logChangeAsyncCalled = true;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimService.RejectClaim(claimId, rejectClaimRequest);

            // Assert
            Assert.NotNull(result);
            Assert.True(logChangeAsyncCalled, "LogChangeAsync should have been called");
            _mockClaimRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()), Times.Once);
            _mockMapper.Verify(m => m.Map(rejectClaimRequest, claim), Times.Once);
            _mockMapper.Verify(m => m.Map<RejectClaimResponse>(It.IsAny<ClaimEntity>()), Times.Once);
            
            // Verify the response matches expected values
            Assert.Equal(expectedResponse.Id, result.Id);
            Assert.Equal(expectedResponse.Status, result.Status);
            Assert.Equal(expectedResponse.Remark, result.Remark);
        }
    }
}