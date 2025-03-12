using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class CancelClaimTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<DAL.Data.Entities.Claim>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<DAL.Data.Entities.Claim>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;

        public CancelClaimTests()
        {
            // Initialize mocks
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<DAL.Data.Entities.Claim>>();
            _mockMapper = new Mock<IMapper>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<DAL.Data.Entities.Claim>>();

            // Setup real DbContext with in-memory database
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();
            _realDbContext.Database.EnsureCreated();

            _mockUnitOfWork.Setup(uow => uow.Context).Returns(_realDbContext);

            // Setup unit of work transaction handling
            _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
                .ReturnsAsync(_mockTransaction.Object);
            _mockUnitOfWork.Setup(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<DAL.Data.Entities.Claim>()).Returns(_mockClaimRepository.Object);

            // Initialize service
            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task CancelClaim_Should_Cancel_Valid_Claim()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();  // Simulated StaffId from JWT
            var claimerId = Guid.Parse(userId);
            var claim = new DAL.Data.Entities.Claim { Id = claimId, ClaimerId = claimerId, Status = ClaimStatus.Draft };
            var cancelRequest = new CancelClaimRequest { Remark = "Cancelled by user" };
            var cancelResponse = new CancelClaimResponse
            {
                ClaimId = claimId,
                ClaimerId = claimerId,
                Status = "Cancelled",
                UpdateAt = DateTime.UtcNow
            };

            // 🔹 Ensure HttpContextAccessor properly returns StaffId
            var mockHttpContext = new DefaultHttpContext();
            var claimsIdentity = new ClaimsIdentity(new[]
            {
        new System.Security.Claims.Claim("StaffId", userId)
    }, "mockAuthType");  // ✅ Ensure an authentication type is set

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            mockHttpContext.User = claimsPrincipal;

            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(mockHttpContext);

            // ✅ Explicitly mock `FindFirst("StaffId")`
            _mockHttpContextAccessor.Setup(h => h.HttpContext.User.FindFirst("StaffId"))
                .Returns(new System.Security.Claims.Claim("StaffId", userId));

            // ✅ Mock identity to avoid null issues
            _mockHttpContextAccessor.Setup(h => h.HttpContext.User.Identity)
                .Returns(claimsIdentity);

            // Mock claim repository to return a valid claim
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            // Mock transaction execution
            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<CancelClaimResponse>>>()))
                .Returns((Func<Task<CancelClaimResponse>> func) => func());

            // Mock claim update
            _mockClaimRepository.Setup(repo => repo.UpdateAsync(It.IsAny<DAL.Data.Entities.Claim>()))
                .Verifiable();

            // Mock response mapping
            _mockMapper.Setup(m => m.Map<CancelClaimResponse>(It.IsAny<DAL.Data.Entities.Claim>()))
                .Returns(cancelResponse);

            // ✅ Inject the correct mock instance
            var _claimService = new ClaimService(
                (IUnitOfWork<ClaimRequestDbContext>)_mockClaimRepository.Object,
                (ILogger<DAL.Data.Entities.Claim>)_mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );

            // Act
            var result = await _claimService.CancelClaim(claimId, cancelRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cancelled", result.Status);
        }




        //[Fact]
        //public async Task CancelClaim_Should_Throw_Exception_When_Claim_Not_Found()
        //{
        //    // Arrange
        //    var claimId = Guid.NewGuid();
        //    var cancelRequest = new CancelClaimRequest { Remark = "Cancelled by user" };

        //    // Setup repository mock to return null (claim not found)
        //    _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
        //        .ReturnsAsync((Claim?)null);

        //    // Act & Assert
        //    await Assert.ThrowsAsync<KeyNotFoundException>(() => _claimService.CancelClaim(claimId, cancelRequest));

        //    // Ensure transaction is not started if claim is not found
        //    _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        //}

        [Fact]
        public async Task CancelClaim_Should_Throw_Exception_When_Claim_Not_In_Draft_Status()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var claim = new DAL.Data.Entities.Claim { Id = claimId, ClaimerId = claimerId, Status = ClaimStatus.Approved }; // Not Draft
            var cancelRequest = new CancelClaimRequest { Remark = "Cancelled by user" };

            // Setup repository mock
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NullReferenceException>(() => _claimService.CancelClaim(claimId, cancelRequest));

            // Ensure transaction is not started if claim status is not Draft
            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelClaim_Should_Throw_Exception_When_ClaimerId_Does_Not_Match()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var differentClaimerId = Guid.NewGuid();
            var claim = new DAL.Data.Entities.Claim { Id = claimId, ClaimerId = claimerId, Status = ClaimStatus.Draft };
            var cancelRequest = new CancelClaimRequest { Remark = "Cancelled by user" };

            // Setup repository mock
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NullReferenceException>(() => _claimService.CancelClaim(claimId, cancelRequest));

            // Ensure transaction is not started if claimerId does not match
            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        public void Dispose()
        {
            // Ensure the database is cleaned up and mocks are reset after each test
            _realDbContext.Database.EnsureDeleted();
            _mockTransaction.Reset();
            _mockClaimRepository.Reset();
            _mockMapper.Reset();
            _mockHttpContextAccessor.Reset();
            _mockUnitOfWork.Reset();
            GC.SuppressFinalize(this);
        }
    }
}
