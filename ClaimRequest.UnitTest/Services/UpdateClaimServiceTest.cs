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
    public class UpdateClaimServiceTest : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<Claim>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IGenericRepository<Claim>> _mockClaimRepository;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;

        public UpdateClaimServiceTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<Claim>>();
            _mockMapper = new Mock<IMapper>();
            _mockTransaction = new Mock<IDbContextTransaction>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimRepository = new Mock<IGenericRepository<Claim>>();

            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();
            _realDbContext.Database.EnsureCreated();

            _mockUnitOfWork.Setup(uow => uow.Context).Returns(_realDbContext);
            _mockUnitOfWork.Setup(uow => uow.BeginTransactionAsync())
                .ReturnsAsync(_mockTransaction.Object);
            _mockUnitOfWork.Setup(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(uow => uow.GetRepository<Claim>()).Returns(_mockClaimRepository.Object);

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task UpdateClaim_Should_Update_Valid_Claim()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var existingClaim = new Claim
            {
                Id = claimId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalWorkingHours = 8
            };

            var updateRequest = new UpdateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Updated Claim",
                Remark = "Updated Remark",
                Amount = 1000,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                TotalWorkingHours = 10
            };

            var updateResponse = new UpdateClaimResponse
            {
                ClaimType = updateRequest.ClaimType,
                Name = updateRequest.Name,
                Remark = updateRequest.Remark,
                Amount = updateRequest.Amount,
                StartDate = updateRequest.StartDate,
                EndDate = updateRequest.EndDate,
                TotalWorkingHours = updateRequest.TotalWorkingHours,
                UpdateAt = DateTime.UtcNow
            };

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(existingClaim);
            _mockMapper.Setup(m => m.Map(updateRequest, existingClaim)).Verifiable();
            _mockMapper.Setup(m => m.Map<UpdateClaimResponse>(It.IsAny<Claim>()))
                .Returns(updateResponse);

            // Act
            var result = await _claimService.UpdateClaim(claimId, updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateResponse.ClaimType, result.ClaimType);
            Assert.Equal(updateResponse.Name, result.Name);
            Assert.Equal(updateResponse.Remark, result.Remark);
            Assert.Equal(updateResponse.Amount, result.Amount);
            Assert.Equal(updateResponse.StartDate, result.StartDate);
            Assert.Equal(updateResponse.EndDate, result.EndDate);
            Assert.Equal(updateResponse.TotalWorkingHours, result.TotalWorkingHours);
            Assert.Equal(updateResponse.UpdateAt, result.UpdateAt);

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateClaim_Should_Throw_Exception_When_Claim_Not_Found()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var updateRequest = new UpdateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Updated Claim",
                Remark = "Updated Remark",
                Amount = 1000,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                TotalWorkingHours = 10
            };

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync((Claim?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _claimService.UpdateClaim(claimId, updateRequest));

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateClaim_Should_Throw_Exception_When_Invalid_Request()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var existingClaim = new Claim
            {
                Id = claimId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalWorkingHours = 8
            };

            var updateRequest = new UpdateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Updated Claim",
                Remark = "Updated Remark",
                Amount = 1000,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), // Invalid: StartDate >= EndDate
                TotalWorkingHours = 10
            };

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(existingClaim);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.UpdateClaim(claimId, updateRequest));

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateClaim_Should_Throw_Exception_When_UpdateRequest_Is_Null()
        {
            // Arrange
            var claimId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _claimService.UpdateClaim(claimId, null!));

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateClaim_Should_Throw_Exception_When_Amount_Is_Negative()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var existingClaim = new Claim
            {
                Id = claimId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalWorkingHours = 8
            };

            var updateRequest = new UpdateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Updated Claim",
                Remark = "Updated Remark",
                Amount = -1000, // Invalid: Negative amount
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                TotalWorkingHours = 10
            };

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(existingClaim);

            // Act & Assert
            await Assert.ThrowsAsync<ClaimRequest.DAL.Data.Exceptions.ValidationException>(() => _claimService.UpdateClaim(claimId, updateRequest));

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateClaim_Should_Throw_Exception_When_TotalWorkingHours_Is_Negative()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var existingClaim = new Claim
            {
                Id = claimId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalWorkingHours = 8
            };

            var updateRequest = new UpdateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Updated Claim",
                Remark = "Updated Remark",
                Amount = 1000,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                TotalWorkingHours = -10 // Invalid: Negative total working hours
            };

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(existingClaim);

            // Act & Assert
            await Assert.ThrowsAsync<ClaimRequest.DAL.Data.Exceptions.ValidationException>(() => _claimService.UpdateClaim(claimId, updateRequest));

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateClaim_Should_Update_Valid_Claim_With_Minimum_Values()
        {
            // Arrange
            var claimId = Guid.NewGuid();
            var existingClaim = new Claim
            {
                Id = claimId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                TotalWorkingHours = 8
            };

            var updateRequest = new UpdateClaimRequest
            {
                ClaimType = ClaimType.HardwareRequest,
                Name = "Updated Claim",
                Remark = "Updated Remark",
                Amount = 0, // Minimum valid amount
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                TotalWorkingHours = 0 // Minimum valid total working hours
            };

            var updateResponse = new UpdateClaimResponse
            {
                ClaimType = updateRequest.ClaimType,
                Name = updateRequest.Name,
                Remark = updateRequest.Remark,
                Amount = updateRequest.Amount,
                StartDate = updateRequest.StartDate,
                EndDate = updateRequest.EndDate,
                TotalWorkingHours = updateRequest.TotalWorkingHours,
                UpdateAt = DateTime.UtcNow
            };

            _mockClaimRepository.Setup(repo => repo.GetByIdAsync(claimId))
                .ReturnsAsync(existingClaim);
            _mockMapper.Setup(m => m.Map(updateRequest, existingClaim)).Verifiable();
            _mockMapper.Setup(m => m.Map<UpdateClaimResponse>(It.IsAny<Claim>()))
                .Returns(updateResponse);

            // Act
            var result = await _claimService.UpdateClaim(claimId, updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateResponse.ClaimType, result.ClaimType);
            Assert.Equal(updateResponse.Name, result.Name);
            Assert.Equal(updateResponse.Remark, result.Remark);
            Assert.Equal(updateResponse.Amount, result.Amount);
            Assert.Equal(updateResponse.StartDate, result.StartDate);
            Assert.Equal(updateResponse.EndDate, result.EndDate);
            Assert.Equal(updateResponse.TotalWorkingHours, result.TotalWorkingHours);
            Assert.Equal(updateResponse.UpdateAt, result.UpdateAt);

            _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitTransactionAsync(It.IsAny<IDbContextTransaction>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        public void Dispose()
        {
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
