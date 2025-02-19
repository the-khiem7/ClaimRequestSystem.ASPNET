using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class CancelClaimTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;

        public CancelClaimTests()
        {
            // Initialize mocks
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockMapper = new Mock<IMapper>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();

            // Setup real DbContext with in-memory database
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")  // Use an in-memory database for unit testing
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();  // Ensure database is clean before each test
            _realDbContext.Database.EnsureCreated();  // Create a new database for each test

            _mockUnitOfWork.Setup(uow => uow.Context).Returns(_realDbContext);

            // Setup unit of work
            _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
                .ReturnsAsync(_mockTransaction.Object);
            _mockUnitOfWork.Setup(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>()).Returns(_mockClaimRepository.Object);

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
            var claimerId = Guid.NewGuid();
            var claim = new Claim { Id = claimId, ClaimerId = claimerId, Status = ClaimStatus.Draft };
            var cancelRequest = new CancelClaimRequest { ClaimId = claimId, ClaimerId = claimerId };
            var cancelResponse = new CancelClaimResponse
            {
                ClaimId = claimId,
                ClaimerId = claimerId,
                Status = "Cancelled",
                UpdateAt = DateTime.UtcNow
            };

            // Setup repository mock
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);
            _mockMapper.Setup(m => m.Map<CancelClaimResponse>(It.IsAny<Claim>()))
                .Returns(cancelResponse);

            // Act
            var result = await _claimService.CancelClaim(cancelRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Cancelled", result.Status);

            // Ensure transactions and commit were handled properly
            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Never);
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CancelClaim_Should_Throw_Exception_When_Claim_Not_Found()
        {
            // Arrange
            var cancelRequest = new CancelClaimRequest
            {
                ClaimId = Guid.NewGuid(),
                ClaimerId = Guid.NewGuid()
            };

            // Setup repository mock to return null (claim not found)
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(cancelRequest.ClaimId))
                .ReturnsAsync((Claim?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _claimService.CancelClaim(cancelRequest));

            // Ensure transaction is not started if claim is not found
            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelClaim_Should_Throw_Exception_When_Claim_Not_In_Draft_Status()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var claimerId = Guid.NewGuid();
            var claim = new Claim { Id = claimId, ClaimerId = claimerId, Status = ClaimStatus.Approved }; // Not Draft
            var cancelRequest = new CancelClaimRequest { ClaimId = claimId, ClaimerId = claimerId };

            // Setup repository mock
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.CancelClaim(cancelRequest));
            Assert.Equal("Claim cannot be cancelled as it is not in Draft status.", exception.Message);

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
            var claim = new Claim { Id = claimId, ClaimerId = claimerId, Status = ClaimStatus.Draft };
            var cancelRequest = new CancelClaimRequest { ClaimId = claimId, ClaimerId = differentClaimerId };

            // Setup repository mock
            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(claim);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _claimService.CancelClaim(cancelRequest));
            Assert.Equal("Claim cannot be cancelled as you are not the claimer.", exception.Message);

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