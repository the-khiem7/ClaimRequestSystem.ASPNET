using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using OfficeOpenXml;
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
        private readonly Claim _fakeClaim;

        public ClaimServiceTests()
        {
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")   // Use an in-memory database
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();  // Clean DB before each test
            _realDbContext.Database.EnsureCreated();  // Recreate DB before each test

            _unitOfWorkMock = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _loggerMock = new Mock<ILogger<Claim>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _claimRepositoryMock = new Mock<IGenericRepository<Claim>>();

            _unitOfWorkMock.Setup(u => u.Context).Returns(_realDbContext);
            _unitOfWorkMock.Setup(u => u.GetRepository<Claim>()).Returns(_claimRepositoryMock.Object);

            _claimService = new ClaimService(_unitOfWorkMock.Object, _loggerMock.Object, _mapperMock.Object, _httpContextAccessorMock.Object);

            _fakeClaim = new Claim
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
        }

        public void Dispose()
        {
            _realDbContext.Dispose();
        }

        [Theory]
        [InlineData(null)] // Null request object
        [InlineData("")] // Empty request
        public async Task DownloadClaimAsync_ShouldThrowNotFoundException_WhenInvalidClaimsProvided(string caseType)
        {
            // Arrange
            var request = caseType == null ? null : new DownloadClaimRequest { ClaimIds = new List<Guid>() };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowNotFoundException_WhenNoClaimsFound()
        {
            // Arrange
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim>()); // Directly return an empty list

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
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim> { _fakeClaim });

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { _fakeClaim.Id } };

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

            // Verify logging
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
        public async Task DownloadClaimAsync_ShouldThrowInvalidOperationException_WhenClaimStatusIsNotPaid()
        {
            // Arrange
            var claimWithDifferentStatus = _fakeClaim;
            claimWithDifferentStatus.Status = ClaimStatus.Pending; 

            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim> { claimWithDifferentStatus });

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { claimWithDifferentStatus.Id } };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowInvalidOperationException_WhenClaimIsNotFromCurrentMonth()
        {
            // Arrange
            var outdatedClaim = _fakeClaim;
            outdatedClaim.UpdateAt = DateTime.UtcNow.AddMonths(-1); // Previous month

            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim> { outdatedClaim });

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { outdatedClaim.Id } };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldFillMissingFields_WhenClaimsHaveNullValues()
        {
            // Arrange
            var claimWithMissingFields = new Claim
            {
                Id = Guid.NewGuid(),
                Claimer = null, 
                Project = null, 
                Finance = null, 
                Name = null, 
                Status = ClaimStatus.Paid,
                UpdateAt = DateTime.UtcNow
            };

            // Mock repository behavior
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim> { claimWithMissingFields });

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { claimWithMissingFields.Id } };

            // Act
            var result = await _claimService.DownloadClaimAsync(request);

            // Assert
            Assert.NotNull(result);
            _loggerMock.Verify(
                log => log.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("has missing fields")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ), Times.Once);
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldGenerateValidExcelFile_WhenClaimsAreValid()
        {
            // Arrange
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(new List<Claim> { _fakeClaim });

            var request = new DownloadClaimRequest { ClaimIds = new List<Guid> { _fakeClaim.Id } };

            // Act
            var result = await _claimService.DownloadClaimAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MemoryStream>(result);

            // Verify Excel content
            result.Position = 0;
            using var package = new ExcelPackage(result);
            var worksheet = package.Workbook.Worksheets["Template Export Claim"];

            Assert.NotNull(worksheet);
            Assert.Equal("Claim ID", worksheet.Cells[1, 1].Value.ToString());
            Assert.Equal(_fakeClaim.Claimer.Name, worksheet.Cells[2, 2].Value.ToString());
            Assert.Equal(_fakeClaim.Project.Name, worksheet.Cells[2, 3].Value.ToString());
            Assert.Equal(_fakeClaim.ClaimType.ToString(), worksheet.Cells[2, 4].Value.ToString());
            Assert.Equal(_fakeClaim.Status.ToString(), worksheet.Cells[2, 5].Value.ToString());
            Assert.Equal(_fakeClaim.Amount, worksheet.Cells[2, 6].GetValue<decimal>());
            Assert.Equal(_fakeClaim.TotalWorkingHours, worksheet.Cells[2, 7].GetValue<int>());
            Assert.Equal(_fakeClaim.StartDate.ToString("yyyy-MM-dd"), worksheet.Cells[2, 8].Value.ToString());
            Assert.Equal(_fakeClaim.EndDate.ToString("yyyy-MM-dd"), worksheet.Cells[2, 9].Value.ToString());
            Assert.Equal(_fakeClaim.CreateAt.ToString("yyyy-MM-dd HH:mm:ss"), worksheet.Cells[2, 10].Value.ToString());
            Assert.Equal(_fakeClaim.Finance.Name, worksheet.Cells[2, 11].Value.ToString());
            Assert.Equal(_fakeClaim.Remark ?? "N/A", worksheet.Cells[2, 12].Value.ToString());
        }
    }
}
