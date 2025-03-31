using System.Security.Claims;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.UnitTest.Services
{
    public class PaidClaimServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _unitOfWorkMock;
        private readonly Mock<ILogger<ClaimEntity>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ClaimService _claimService;
        private readonly Guid _validClaimId;
        private readonly Guid _validFinanceId;
        private readonly Guid _validUserId;

        public PaidClaimServiceTests()
        {
            // Setup mocks
            _unitOfWorkMock = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _loggerMock = new Mock<ILogger<ClaimEntity>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Setup valid IDs
            _validClaimId = Guid.NewGuid();
            _validFinanceId = Guid.NewGuid();
            _validUserId = Guid.NewGuid();

            // Setup HttpContext with user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("StaffId", _validUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Create service instance
            _claimService = new ClaimService(
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _mapperMock.Object,
                _httpContextAccessorMock.Object
            );
        }

        [Fact]
            public async Task PaidClaim_WithApprovedStatusAndFinanceRole_ShouldSucceed()
        {
            // Arrange
            var claim = new ClaimEntity
            {
                Id = _validClaimId,
                Status = ClaimStatus.Approved,
                ClaimerId = _validUserId
            };

            var finance = new Staff
            {
                Id = _validFinanceId,
                SystemRole = SystemRole.Finance,
                Name = "Test Finance"
            };

            var claimRepoMock = new Mock<IGenericRepository<ClaimEntity>>();
            var staffRepoMock = new Mock<IGenericRepository<Staff>>();
            var changeLogRepoMock = new Mock<IGenericRepository<ClaimChangeLog>>();

            claimRepoMock.Setup(x => x.GetByIdAsync(_validClaimId))
                .ReturnsAsync(claim);
            staffRepoMock.Setup(x => x.GetByIdAsync(_validFinanceId))
                .ReturnsAsync(finance);
            changeLogRepoMock.Setup(x => x.InsertAsync(It.IsAny<ClaimChangeLog>()))
                .Returns(Task.FromResult(new ClaimChangeLog()));

            _unitOfWorkMock.Setup(x => x.GetRepository<ClaimEntity>())
                .Returns(claimRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Staff>())
                .Returns(staffRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<ClaimChangeLog>())
                .Returns(changeLogRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.CommitAsync())
                .ReturnsAsync(1);
            _unitOfWorkMock.Setup(x => x.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(async func => 
                {
                    var result = await func();
                    await _unitOfWorkMock.Object.CommitAsync();
                    return result;
                });

            // Act
            var result = await _claimService.PaidClaim(_validClaimId, _validFinanceId);

            // Assert
            Assert.True(result);
            Assert.Equal(ClaimStatus.Paid, claim.Status);
            Assert.Equal(_validFinanceId, claim.FinanceId);
            claimRepoMock.Verify(x => x.UpdateAsync(claim), Times.Once);
            changeLogRepoMock.Verify(x => x.InsertAsync(It.IsAny<ClaimChangeLog>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task PaidClaim_WithApprovedStatusButNonFinanceRole_ShouldThrowException()
        {
            // Arrange
            var claim = new ClaimEntity
            {
                Id = _validClaimId,
                Status = ClaimStatus.Approved,
                ClaimerId = _validUserId
            };

            var staff = new Staff
            {
                Id = _validFinanceId,
                SystemRole = SystemRole.Admin,
                Name = "Test Admin"
            };

            var claimRepoMock = new Mock<IGenericRepository<ClaimEntity>>();
            var staffRepoMock = new Mock<IGenericRepository<Staff>>();

            claimRepoMock.Setup(x => x.GetByIdAsync(_validClaimId))
                .ReturnsAsync(claim);
            staffRepoMock.Setup(x => x.GetByIdAsync(_validFinanceId))
                .ReturnsAsync(staff);

            _unitOfWorkMock.Setup(x => x.GetRepository<ClaimEntity>())
                .Returns(claimRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Staff>())
                .Returns(staffRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(func => func());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _claimService.PaidClaim(_validClaimId, _validFinanceId));
            Assert.Contains("The user does not have permission to paid this claim", exception.Message);
        }

        [Fact]
        public async Task PaidClaim_WithNonApprovedStatus_ShouldThrowException()
        {
            // Arrange
            var claim = new ClaimEntity
            {
                Id = _validClaimId,
                Status = ClaimStatus.Draft,
                ClaimerId = _validUserId
            };

            var finance = new Staff
            {
                Id = _validFinanceId,
                SystemRole = SystemRole.Finance,
                Name = "Test Finance"
            };

            var claimRepoMock = new Mock<IGenericRepository<ClaimEntity>>();
            var staffRepoMock = new Mock<IGenericRepository<Staff>>();

            claimRepoMock.Setup(x => x.GetByIdAsync(_validClaimId))
                .ReturnsAsync(claim);
            staffRepoMock.Setup(x => x.GetByIdAsync(_validFinanceId))
                .ReturnsAsync(finance);

            _unitOfWorkMock.Setup(x => x.GetRepository<ClaimEntity>())
                .Returns(claimRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Staff>())
                .Returns(staffRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(func => func());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BusinessException>(() =>
                _claimService.PaidClaim(_validClaimId, _validFinanceId));
            Assert.Contains("Cannot mark as Paid when the status is not Approved", exception.Message);
        }

        [Fact]
        public async Task PaidClaim_WithNonExistentClaim_ShouldThrowException()
        {
            // Arrange
            var claimRepoMock = new Mock<IGenericRepository<ClaimEntity>>();
            claimRepoMock.Setup(x => x.GetByIdAsync(_validClaimId))
                .ReturnsAsync((ClaimEntity)null);

            _unitOfWorkMock.Setup(x => x.GetRepository<ClaimEntity>())
                .Returns(claimRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(func => func());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _claimService.PaidClaim(_validClaimId, _validFinanceId));
            Assert.Contains("Claim not found", exception.Message);
        }

        [Fact]
        public async Task PaidClaim_WithNonExistentFinance_ShouldThrowException()
        {
            // Arrange
            var claim = new ClaimEntity
            {
                Id = _validClaimId,
                Status = ClaimStatus.Approved,
                ClaimerId = _validUserId
            };

            var claimRepoMock = new Mock<IGenericRepository<ClaimEntity>>();
            var staffRepoMock = new Mock<IGenericRepository<Staff>>();

            claimRepoMock.Setup(x => x.GetByIdAsync(_validClaimId))
                .ReturnsAsync(claim);
            staffRepoMock.Setup(x => x.GetByIdAsync(_validFinanceId))
                .ReturnsAsync((Staff)null);

            _unitOfWorkMock.Setup(x => x.GetRepository<ClaimEntity>())
                .Returns(claimRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.GetRepository<Staff>())
                .Returns(staffRepoMock.Object);
            _unitOfWorkMock.Setup(x => x.ProcessInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns<Func<Task<bool>>>(func => func());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _claimService.PaidClaim(_validClaimId, _validFinanceId));
            Assert.Contains("Finance not found", exception.Message);
        }
    }
}
