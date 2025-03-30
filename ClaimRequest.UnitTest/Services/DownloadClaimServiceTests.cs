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
using OfficeOpenXml;
using System.Linq.Expressions;
using Xunit;

namespace ClaimRequest.Tests.Services
{
    public class ClaimServiceTests : IDisposable
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _unitOfWorkMock;
        private readonly Mock<ILogger<Claim>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IGenericRepository<Claim>> _claimRepositoryMock;
        private readonly ClaimService _claimService;
        private readonly ClaimRequestDbContext _realDbContext;

        public ClaimServiceTests()
        {
            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _unitOfWorkMock = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _loggerMock = new Mock<ILogger<Claim>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _claimRepositoryMock = new Mock<IGenericRepository<Claim>>();

            _unitOfWorkMock.Setup(u => u.Context).Returns(_realDbContext);
            _unitOfWorkMock.Setup(u => u.GetRepository<Claim>()).Returns(_claimRepositoryMock.Object);

            _claimService = new ClaimService(_unitOfWorkMock.Object, _loggerMock.Object, _mapperMock.Object, _httpContextAccessorMock.Object);

            // Setup default repository behavior
            SetupClaimRepositoryMock(new List<Claim>());
        }

        public void Dispose() => _realDbContext.Dispose();

        private Claim CreateFakeClaim() => new Claim
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

        private void SetupClaimRepositoryMock(List<Claim> claims) =>
            _claimRepositoryMock.Setup(repo => repo.GetListAsync(
                It.IsAny<Expression<Func<Claim, bool>>>(),
                It.IsAny<Func<IQueryable<Claim>, IOrderedQueryable<Claim>>>(),
                It.IsAny<Func<IQueryable<Claim>, IIncludableQueryable<Claim, object>>>()
            )).ReturnsAsync(claims);

        private DownloadClaimRequest CreateRequest(Guid claimId) =>
            new DownloadClaimRequest { ClaimIds = new List<Guid> { claimId } };

        private async Task AssertThrowsAsync<TException>(Func<Task> action, string expectedMessage = null) where TException : Exception
        {
            var exception = await Assert.ThrowsAsync<TException>(action);
            if (expectedMessage != null)
                Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData(null, "Null request")]
        [InlineData("", "Empty request")]
        public async Task DownloadClaimAsync_ShouldThrowNotFoundException_WhenRequestIsInvalid(string caseType, string scenario)
        {
            // Arrange
            var request = caseType == null ? null : new DownloadClaimRequest { ClaimIds = new List<Guid>() };

            // Act & Assert
            await AssertThrowsAsync<NotFoundException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowNotFoundException_WhenNoClaimsFound()
        {
            // Arrange
            var request = CreateRequest(Guid.NewGuid());

            // Act & Assert
            await AssertThrowsAsync<NotFoundException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowInvalidOperationException_WhenDatabaseContextIsNull()
        {
            // Arrange
            _unitOfWorkMock.Setup(u => u.Context).Returns((ClaimRequestDbContext)null);
            var request = CreateRequest(Guid.NewGuid());

            // Act & Assert
            await AssertThrowsAsync<InvalidOperationException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldReturnMemoryStream_WhenClaimsFound()
        {
            // Arrange
            var fakeClaim = CreateFakeClaim();
            SetupClaimRepositoryMock(new List<Claim> { fakeClaim });
            var request = CreateRequest(fakeClaim.Id);

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
            var request = CreateRequest(Guid.NewGuid());

            // Act & Assert
            await AssertThrowsAsync<Exception>(() => _claimService.DownloadClaimAsync(request), "Database error");
            VerifyLogger(LogLevel.Error, "Error downloading claim");
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowInvalidOperationException_WhenClaimStatusIsNotPaid()
        {
            // Arrange
            var claim = CreateFakeClaim();
            claim.Status = ClaimStatus.Pending;
            SetupClaimRepositoryMock(new List<Claim> { claim });
            var request = CreateRequest(claim.Id);

            // Act & Assert
            await AssertThrowsAsync<InvalidOperationException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldThrowInvalidOperationException_WhenClaimIsNotFromCurrentMonth()
        {
            // Arrange
            var claim = CreateFakeClaim();
            claim.UpdateAt = DateTime.UtcNow.AddMonths(-1);
            SetupClaimRepositoryMock(new List<Claim> { claim });
            var request = CreateRequest(claim.Id);

            // Act & Assert
            await AssertThrowsAsync<InvalidOperationException>(() => _claimService.DownloadClaimAsync(request));
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldFillMissingFields_WhenClaimsHaveNullValues()
        {
            // Arrange
            var claim = new Claim
            {
                Id = Guid.NewGuid(),
                Claimer = null,
                Project = null,
                Finance = null,
                Name = null,
                Status = ClaimStatus.Paid,
                UpdateAt = DateTime.UtcNow
            };
            SetupClaimRepositoryMock(new List<Claim> { claim });
            var request = CreateRequest(claim.Id);

            // Act
            var result = await _claimService.DownloadClaimAsync(request);

            // Assert
            Assert.NotNull(result);
            VerifyLogger(LogLevel.Warning, "has missing fields");
        }

        [Fact]
        public async Task DownloadClaimAsync_ShouldGenerateValidExcelFile_WhenClaimsAreValid()
        {
            // Arrange
            var fakeClaim = CreateFakeClaim();
            SetupClaimRepositoryMock(new List<Claim> { fakeClaim });
            var request = CreateRequest(fakeClaim.Id);

            // Act
            var result = await _claimService.DownloadClaimAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MemoryStream>(result);

            result.Position = 0;
            using var package = new ExcelPackage(result);
            var worksheet = package.Workbook.Worksheets["Template Export Claim"];

            Assert.NotNull(worksheet);
            Assert.Equal("Claim ID", worksheet.Cells[1, 1].Value.ToString());
            Assert.Equal(fakeClaim.Claimer.Name, worksheet.Cells[2, 2].Value.ToString());
            Assert.Equal(fakeClaim.Project.Name, worksheet.Cells[2, 3].Value.ToString());
            Assert.Equal(fakeClaim.ClaimType.ToString(), worksheet.Cells[2, 4].Value.ToString());
            Assert.Equal(fakeClaim.Status.ToString(), worksheet.Cells[2, 5].Value.ToString());
            Assert.Equal(fakeClaim.Amount, worksheet.Cells[2, 6].GetValue<decimal>());
            Assert.Equal(fakeClaim.TotalWorkingHours, worksheet.Cells[2, 7].GetValue<int>());
            Assert.Equal(fakeClaim.StartDate.ToString("yyyy-MM-dd"), worksheet.Cells[2, 8].Value.ToString());
            Assert.Equal(fakeClaim.EndDate.ToString("yyyy-MM-dd"), worksheet.Cells[2, 9].Value.ToString());
            Assert.Equal(fakeClaim.CreateAt.ToString("yyyy-MM-dd HH:mm:ss"), worksheet.Cells[2, 10].Value.ToString());
            Assert.Equal(fakeClaim.Finance.Name, worksheet.Cells[2, 11].Value.ToString());
            Assert.Equal(fakeClaim.Remark ?? "N/A", worksheet.Cells[2, 12].Value.ToString());
        }

        private void VerifyLogger(LogLevel level, string messageContains) =>
            _loggerMock.Verify(log => log.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(messageContains)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
    }
}