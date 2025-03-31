using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using Xunit.Abstractions;
using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.UnitTest.Services
{
    public class CancelClaimTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<ClaimEntity>> _mockClaimRepository;
        private readonly ClaimService _claimService;

        public CancelClaimTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<ClaimEntity>>();

            _mockUnitOfWork.Setup(uow => uow.GetRepository<ClaimEntity>()).Returns(_mockClaimRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<CancelClaimResponse>>>()))
                .Returns<Func<Task<CancelClaimResponse>>>(operation => operation());

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        public void Dispose() => GC.SuppressFinalize(this);

        private ClaimsPrincipal CreateClaimsPrincipal(Guid userId) =>
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new System.Security.Claims.Claim("StaffId", userId.ToString()) }));
        private void LogTestStart(string testName)
        {
            _testOutputHelper.WriteLine($"[Test Start] {testName}");
        }

        private void LogTestFinish(string testName)
        {
            _testOutputHelper.WriteLine($"[Test Finish] {testName}");
        }

        [Fact]
        public async Task CancelClaim_ShouldReturn_CancelClaimResponse_WhenSuccessful()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "User requested cancellation" };
            var claim = new ClaimEntity { Id = claimId, ClaimerId = userId, Status = ClaimStatus.Draft };
            var expectedResponse = new CancelClaimResponse
            {
                ClaimId = claimId,
                Status = ClaimStatus.Cancelled.ToString(),
                Remark = cancelClaimRequest.Remark
            };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(userId) });
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync(claim);
            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>())).Verifiable();
            _mockMapper.Setup(m => m.Map<CancelClaimResponse>(It.IsAny<ClaimEntity>())).Returns(expectedResponse);

            // Act
            var result = await _claimService.CancelClaim(claimId, cancelClaimRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Status, result.Status);
            Assert.Equal(expectedResponse.Remark, result.Remark);
            _mockClaimRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ClaimEntity>()), Times.Once);
        }

        [Fact]
        public async Task CancelClaim_ShouldThrowException_WhenClaimNotFound()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "Attempt to cancel non-existing claim" };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(userId) });
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync((ClaimEntity)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _claimService.CancelClaim(claimId, cancelClaimRequest));
            Assert.Equal("Claim not found.", exception.Message);
        }

        [Fact]
        public async Task CancelClaim_ShouldThrowException_WhenClaimCannotBeCancelled()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "User attempted cancellation" };
            var claim = new ClaimEntity { Id = claimId, ClaimerId = userId, Status = ClaimStatus.Approved };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(userId) });
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.CancelClaim(claimId, cancelClaimRequest));
            Assert.Equal("Claim cannot be cancelled as it is not in Draft status.", exception.Message);
        }

        [Fact]
        public async Task CancelClaim_ShouldThrowException_OnTransactionFailure()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "Simulate error during cancellation" };
            var claim = new ClaimEntity { Id = claimId, ClaimerId = userId, Status = ClaimStatus.Draft };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(userId) });
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync(claim);
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<CancelClaimResponse>>>()))
                           .ThrowsAsync(new Exception("Simulated Exception"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _claimService.CancelClaim(claimId, cancelClaimRequest));
            Assert.Equal("Simulated Exception", exception.Message);
        }

        [Fact]
        public async Task CancelClaim_ShouldThrowException_WhenUserNotAuthorized()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "Unauthorized cancellation" };
            var claim = new ClaimEntity { Id = claimId, ClaimerId = userId, Status = ClaimStatus.Draft };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(differentUserId) });
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.CancelClaim(claimId, cancelClaimRequest));
            Assert.Equal("Claim cannot be cancelled as you are not the claimer.", exception.Message);
        }

        [Fact]
        public async Task CancelClaim_ShouldThrowException_WhenRequestIsNull()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(userId) });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _claimService.CancelClaim(claimId, null));
            Assert.Equal("Claim not found.", exception.Message);
        }

        [Fact]
        public async Task CancelClaim_ShouldThrowException_WhenUserContextMissing()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "No user context" };
            var claim = new ClaimEntity { Id = claimId, ClaimerId = Guid.NewGuid(), Status = ClaimStatus.Draft };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext()); // No user set
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _claimService.CancelClaim(claimId, cancelClaimRequest));
            Assert.Equal("User ID not found in JWT.", exception.Message);
        }

        [Theory]
        [InlineData(ClaimStatus.Approved)]
        [InlineData(ClaimStatus.Rejected)]
        [InlineData(ClaimStatus.Pending)]
        public async Task CancelClaim_ShouldThrowException_WhenStatusNotDraft(ClaimStatus status)
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cancelClaimRequest = new CancelClaimRequest { Remark = "Invalid status" };
            var claim = new ClaimEntity { Id = claimId, ClaimerId = userId, Status = status };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext { User = CreateClaimsPrincipal(userId) });
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId)).ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.CancelClaim(claimId, cancelClaimRequest));
            Assert.Equal("Claim cannot be cancelled as it is not in Draft status.", exception.Message);
        }
    }
}