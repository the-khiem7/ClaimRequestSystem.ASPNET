using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.UnitTest.Services
{
    public class ApproveClaimServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<ClaimEntity>> _mockClaimRepository;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepository;
        private readonly ClaimService _claimService;

        public ApproveClaimServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();
            _mockProjectRepository = new Mock<IGenericRepository<Project>>();

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimEntity>()).Returns(_mockClaimRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Project>()).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(operation => operation());

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task ApproveClaim_ShouldThrowUnauthorizedException_WhenUserIsNull()
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _claimService.ApproveClaim(null, Guid.NewGuid()));
        }

        [Fact]
        public async Task ApproveClaim_ShouldThrowUnauthorizedException_WhenApproverIdNotFound()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _claimService.ApproveClaim(user, Guid.NewGuid()));
        }
        [Fact]
        public async Task ApproveClaim_ShouldThrowNotFoundException_WhenClaimDoesNotExist()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync((ClaimEntity)null);

            await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _claimService.ApproveClaim(user, claimId));
        }
        [Fact]
        public async Task ApproveClaim_ShouldThrowBadRequestException_WhenClaimIsNotPending()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Approved,
                ClaimApprovers = new List<ClaimApprover>
        {
            new ClaimApprover { ApproverId = approverId }
        }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            await Assert.ThrowsAsync<BadRequestException>(async () =>
                await _claimService.ApproveClaim(user, claimId));
        }

        [Fact]
        public async Task ApproveClaim_ShouldThrowUnauthorizedException_WhenUserIsNotAnApprover()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover>()
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _claimService.ApproveClaim(user, claimId));
        }

        [Fact]
        public async Task ApproveClaim_ShouldApproveClaim_WhenValidApprover()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var financeStaffId = Guid.NewGuid();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ProjectId = projectId,
                ClaimApprovers = new List<ClaimApprover> { new ClaimApprover { ApproverId = approverId } }
            };

            var project = new ClaimRequest.DAL.Data.Entities.Project
            {
                Id = projectId,
                FinanceStaffId = financeStaffId
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()
            )).ReturnsAsync(claim);

            _mockProjectRepository.Setup(repo => repo.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()))
                .Callback<ClaimEntity>(c =>
                {
                    c.Status = ClaimStatus.Approved;
                    c.FinanceId = project?.FinanceStaffId ?? Guid.Empty;
                });

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(operation => operation());

            var result = await _claimService.ApproveClaim(user, claimId);

            Assert.True(result);
            Assert.Equal(ClaimStatus.Approved, claim.Status);
            Assert.Equal(financeStaffId, claim.FinanceId);
        }


        [Fact]
        public async Task ApproveClaim_ShouldThrowUnauthorizedException_WhenClaimHasNoApprovers()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = null
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _claimService.ApproveClaim(user, claimId));
        }

        [Fact]
        public async Task ApproveClaim_ShouldThrowException_WhenTransactionFails()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover> { new ClaimApprover { ApproverId = approverId } }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .ThrowsAsync(new Exception("Transaction failed"));

            await Assert.ThrowsAsync<Exception>(async () =>
                await _claimService.ApproveClaim(user, claimId));
        }

        [Fact]
        public async Task ApproveClaim_ShouldThrowException_WhenUpdateFails()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover> { new ClaimApprover { ApproverId = approverId } }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async (_) => throw new Exception("Transaction failed"));

            await Assert.ThrowsAsync<Exception>(async () =>
                await _claimService.ApproveClaim(user, claimId));
        }

        [Fact]
        public async Task ApproveClaim_ShouldReturnFalse_WhenTransactionFails()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover> { new ClaimApprover { ApproverId = approverId } }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .ReturnsAsync(false);

            var result = await _claimService.ApproveClaim(user, claimId);

            Assert.False(result);
        }

        [Fact]
        public async Task ApproveClaim_ShouldCallUpdateAsync_WhenValidApprover()
        {
            var claimId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", approverId.ToString()) }));

            var claim = new ClaimEntity
            {
                Id = claimId,
                Status = ClaimStatus.Pending,
                ClaimApprovers = new List<ClaimApprover> { new ClaimApprover { ApproverId = approverId } }
            };

            _mockClaimRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<ClaimEntity, bool>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IOrderedQueryable<ClaimEntity>>>(),
                It.IsAny<Func<IQueryable<ClaimEntity>, IIncludableQueryable<ClaimEntity, object>>>()))
                .ReturnsAsync(claim);

            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()))
            .Callback<ClaimEntity>(claim => { });

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(operation => operation());

            var result = await _claimService.ApproveClaim(user, claimId);

            Assert.True(result);
            _mockClaimRepository.Verify(repo => repo.UpdateAsync(It.Is<ClaimEntity>(c => c.Id == claimId)), Times.Once);
        }
    }
}