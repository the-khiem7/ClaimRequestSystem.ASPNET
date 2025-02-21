using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.Tests.Services
{
    public class ClaimServiceTests : IDisposable
    {
        private readonly ClaimRequestDbContext _realDbContext;
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _unitOfWorkMock;
        private readonly Mock<ILogger<Claim>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IGenericRepository<Claim>> _claimRepositoryMock;
        private readonly ClaimService _claimService;

        public ClaimServiceTests()
        {
            // Setup real DbContext with in-memory database
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")  // Use an in-memory database
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();  // Clean DB before each test
            _realDbContext.Database.EnsureCreated();  // Recreate DB before each test

            // Initialize Mocks
            _unitOfWorkMock = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _loggerMock = new Mock<ILogger<Claim>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _claimRepositoryMock = new Mock<IGenericRepository<Claim>>();

            _unitOfWorkMock.Setup(u => u.Context).Returns(_realDbContext);
            _unitOfWorkMock.Setup(u => u.GetRepository<Claim>()).Returns(_claimRepositoryMock.Object);

            _claimService = new ClaimService(_unitOfWorkMock.Object, _loggerMock.Object, _mapperMock.Object, _httpContextAccessorMock.Object);
        }

        public void Dispose()
        {
            _realDbContext.Dispose();

        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowNotFoundException_WhenNoClaimsFound()
        {
            // Arrange
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync((Expression<Func<Claim, bool>> predicate,
                             Func<IQueryable<Claim>, IOrderedQueryable<Claim>> orderBy,
                             Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>> include) =>
            {
                var claims = _realDbContext.Claims.Include(c => c.Claimer)
                                                  .Include(c => c.Project)
                                                  .Include(c => c.Finance)
                                                  .Where(predicate.Compile())
                                                  .ToList();
                return claims;
            });


            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { Guid.NewGuid() } };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowInvalidOperationException_WhenDatabaseContextIsNotInitialized()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Context).Returns((ClaimRequestDbContext)null);
            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { Guid.NewGuid() } };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldReturnMemoryStream_WhenClaimsFound()
        {
            // Arrange
            var fakeClaim = new Claim
            {
                Id = Guid.NewGuid(),
                Claimer = new Staff { Id = Guid.NewGuid(), Name = "John Doe", Email = "JohnDoe@gmail.com", Password = "1" },
                Project = new Project { Id = Guid.NewGuid(), Name = "Project A", Description = "N/A", EndDate = DateOnly.MaxValue },
                Finance = new Staff { Id = Guid.NewGuid(), Name = "Jane Doe", Email = "JaneDoe@gmail.com", Password = "2" },
                Name = "Claim 1",
                ClaimType = ClaimType.OvertimeCompensation,
                Status = ClaimStatus.Paid,
                Amount = 100,
                TotalWorkingHours = 8,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                Remark = "N/A"
            };

            var fakeClaims = new List<Claim> { fakeClaim };

            // Mock GetListAsync to return fake claims
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(fakeClaims);

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { fakeClaim.Id } };

            // Act
            var result = await _claimService.DownloadClaimAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MemoryStream>(result);
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowException_WhenRepositoryFails()
        {
            // Arrange
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ThrowsAsync(new Exception("Database error"));

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { Guid.NewGuid() } };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _claimService.DownloadClaimAsync(request));
            Assert.Equal("Database error", exception.Message);

            // Verify that error logging was called
            _loggerMock.Verify(
                log => log.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error downloading claim")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ), Times.Once);
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldExcludeUnpaidClaims()
        {
            // Arrange
            var fakeClaim = new Claim
            {
                Id = Guid.NewGuid(),
                Claimer = new Staff { Id = Guid.NewGuid(), Name = "John Doe", Email = "JohnDoe@gmail.com", Password = "1" },
                Project = new Project { Id = Guid.NewGuid(), Name = "Project A", Description = "N/A", EndDate = DateOnly.MaxValue },
                Finance = new Staff { Id = Guid.NewGuid(), Name = "Jane Doe", Email = "JaneDoe@gmail.com", Password = "2" },
                Name = "Claim 1",
                ClaimType = ClaimType.OvertimeCompensation,
                Status = ClaimStatus.Paid,
                Amount = 100,
                TotalWorkingHours = 8,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                Remark = "N/A"
            };

            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim>());

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { fakeClaim.Id } };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowNotFoundException_WhenClaimIdsAreEmpty()
        {
            // Arrange
            var request = new DownloadClaimRequest { ClaimIds = new List<Guid>() };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.DownloadClaimAsync(request));
        }


    }
}
