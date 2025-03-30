using System.Security.Claims;
using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    using ClaimEntity = ClaimRequest.DAL.Data.Entities.Claim;

    public class ApproveClaimServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ClaimEntity>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly ClaimRequestDbContext _realDbContext;
        private readonly IClaimService _claimService;

        public ApproveClaimServiceTests()
        {
            _mockLogger = new Mock<ILogger<ClaimEntity>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();

            var options = new DbContextOptionsBuilder<ClaimRequestDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _realDbContext = new ClaimRequestDbContext(options);
            _realDbContext.Database.EnsureDeleted();
            _realDbContext.Database.EnsureCreated();

            _mockUnitOfWork.Setup(u => u.Context).Returns(_realDbContext);

            _claimService = new ClaimService(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }
        [Fact]
        public async Task ApproveClaim_ShouldThrowUnauthorizedAccessException_WhenNoTokenProvided()
        {
            var claimId = Guid.NewGuid();

            // Giả lập không có token (ClaimsPrincipal)
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns((ClaimsPrincipal)null);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _claimService.ApproveClaim(null, claimId));

            Assert.Equal("User is not authorized.", exception.Message);
        }

        public void Dispose()
        {
            _realDbContext.Database.EnsureDeleted();
            _mockHttpContextAccessor.Reset();
            GC.SuppressFinalize(this);
        }
    }
}
