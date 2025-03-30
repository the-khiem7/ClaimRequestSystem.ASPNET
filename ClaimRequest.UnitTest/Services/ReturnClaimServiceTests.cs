using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
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
    public class ReturnClaimServiceTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<ClaimEntity>> _mockClaimRepository;
        private readonly Mock<IGenericRepository<ClaimChangeLog>> _mockClaimChangeLogRepository;
        private readonly ClaimService _claimService;

        public ReturnClaimServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();
            _mockClaimChangeLogRepository = new Mock<IGenericRepository<ClaimChangeLog>>();

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimEntity>()).Returns(_mockClaimRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimChangeLog>()).Returns(_mockClaimChangeLogRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<ReturnClaimResponse>>>()))
                .Returns<Func<Task<ReturnClaimResponse>>>(operation => operation());

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        public void Dispose() => GC.SuppressFinalize(this);

       
        [Fact]
        public async Task ReturnClaim_ShouldThrowException_WhenClaimNotFound()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var returnClaimRequest = new ReturnClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to return non-existing claim"
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync((ClaimEntity)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _claimService.ReturnClaim(claimId, returnClaimRequest));

            Assert.Equal($"Claim with ID {claimId} not found.", exception.Message);
        }

        [Fact]
        public async Task ReturnClaim_ShouldThrowException_WhenClaimNotInPendingStatus()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var returnClaimRequest = new ReturnClaimRequest
            {
                ApproverId = approverId,
                Remark = "Attempt to return a claim not in pending status"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Draft, 
                ClaimApprovers = new List<ClaimApprover>
                {
                    new ClaimApprover { ApproverId = approverId, ClaimId = claimId }
                }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                _claimService.ReturnClaim(claimId, returnClaimRequest));

            Assert.Equal($"Claim with ID {claimId} is not pending for approval.", exception.Message);
        }

        [Fact]
        public async Task ReturnClaim_ShouldThrowException_WhenApproverNotAuthorized()
        {
            var claimId = Guid.NewGuid();
            var realApproverId = Guid.NewGuid();
            var unauthorizedApproverId = Guid.NewGuid();

            var returnClaimRequest = new ReturnClaimRequest
            {
                ApproverId = unauthorizedApproverId,
                Remark = "Attempt to return by unauthorized approver"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover>
                {
                    new ClaimApprover { ApproverId = realApproverId, ClaimId = claimId }
                }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _claimService.ReturnClaim(claimId, returnClaimRequest));

            Assert.Equal($"Approver with ID {unauthorizedApproverId} does not have permission to return claim ID {claimId}.", exception.Message);
        }
        [Fact]
        public async Task ReturnClaim_ShouldThrowException_OnTransactionFailure()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var returnClaimRequest = new ReturnClaimRequest
            {
                ApproverId = approverId,
                Remark = "Simulate transaction failure"
            };

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover>
                {
                    new ClaimApprover { ApproverId = approverId, ClaimId = claimId }
                }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<ReturnClaimResponse>>>()))
                .ThrowsAsync(new Exception("Database transaction failed"));

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _claimService.ReturnClaim(claimId, returnClaimRequest));

            Assert.Equal("Database transaction failed", exception.Message);
        }

        [Fact]
        public async Task ReturnClaim_ShouldUpdateClaimStatus_WhenSuccessful()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var returnClaimRequest = new ReturnClaimRequest
            {
                ApproverId = approverId,
                Remark = "Need clarification"
            };

            ClaimEntity capturedClaim = null;
            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover>
                {
                    new ClaimApprover { ApproverId = approverId, ClaimId = claimId }
                }
            };

            var expectedResponse = new ReturnClaimResponse
            {
                Id = claimId,
                Status = ClaimStatus.Draft,
                Remark = returnClaimRequest.Remark
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()))
                .Callback<ClaimEntity>(c => capturedClaim = c);

            _mockMapper.Setup(m => m.Map(returnClaimRequest, claim))
                .Callback<ReturnClaimRequest, ClaimEntity>((req, c) =>
                {
                    c.Remark = req.Remark;
                });

            _mockMapper.Setup(m => m.Map<ReturnClaimResponse>(It.IsAny<ClaimEntity>())).Returns(expectedResponse);

            var result = await _claimService.ReturnClaim(claimId, returnClaimRequest);

            Assert.NotNull(capturedClaim);
            Assert.Equal(ClaimStatus.Draft, capturedClaim.Status);
            Assert.Equal(returnClaimRequest.Remark, capturedClaim.Remark);
        }
    }
}
