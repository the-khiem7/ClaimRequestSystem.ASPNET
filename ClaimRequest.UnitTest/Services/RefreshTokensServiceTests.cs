using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ClaimRequest.BLL.Tests.Services
{
    public class RefreshTokensServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<IGenericRepository<RefreshTokens>> _mockRefreshTokenRepo;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepo;
        private readonly Mock<IJwtUtil> _mockJwtUtil;
        private readonly RefreshTokensService _refreshTokensService;

        public RefreshTokensServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockRefreshTokenRepo = new Mock<IGenericRepository<RefreshTokens>>();
            _mockStaffRepo = new Mock<IGenericRepository<Staff>>();
            _mockJwtUtil = new Mock<IJwtUtil>();

            _mockJwtUtil.Setup(jwt => jwt.GenerateJwtToken(It.IsAny<Staff>(), It.IsAny<Tuple<string, Guid>>(), It.IsAny<bool>()))
                    .Returns("sample-access-token");

            _mockUnitOfWork.Setup(u => u.GetRepository<RefreshTokens>()).Returns(_mockRefreshTokenRepo.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Staff>()).Returns(_mockStaffRepo.Object);

            _mockUnitOfWork.Setup(uow => uow.ProcessInTransactionAsync(It.IsAny<Func<Task<string>>>()))
                .Returns((Func<Task<string>> func) => func());

            _mockRefreshTokenRepo.Setup(repo => repo.InsertAsync(It.IsAny<RefreshTokens>()))
                .Returns(Task.CompletedTask);

            _refreshTokensService = new RefreshTokensService(_mockUnitOfWork.Object, _mockJwtUtil.Object);
        }

        [Fact]
        public async Task GenerateAndStoreRefreshToken_ShouldReturnRefreshToken()
        {
            var userId = Guid.NewGuid();

            var result = await _refreshTokensService.GenerateAndStoreRefreshToken(userId);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result));

            _mockRefreshTokenRepo.Verify(repo => repo.InsertAsync(It.IsAny<RefreshTokens>()), Times.Once);
        }

        [Fact]
        public async Task RefreshAccessToken_ShouldThrowException_WhenRefreshTokenIsExpired()
        {
            var refreshToken = "expired-refresh-token";
            var userId = Guid.NewGuid();
            var storedRefreshToken = new RefreshTokens
            {
                Token = refreshToken,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };

            _mockRefreshTokenRepo.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RefreshTokens, bool>>>(), null, null))
                .ReturnsAsync(storedRefreshToken);

            await Assert.ThrowsAsync<Exception>(async () =>
                await _refreshTokensService.RefreshAccessToken(refreshToken));
        }

        [Fact]
        public async Task DeleteRefreshToken_ShouldReturnFalse_WhenTokenDoesNotExist()
        {
            var refreshToken = "non-existing-refresh-token";

            _mockRefreshTokenRepo.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<RefreshTokens, bool>>>(), null, null))
                .ReturnsAsync((RefreshTokens?)null);

            var result = await _refreshTokensService.DeleteRefreshToken(refreshToken);

            Assert.False(result);
            _mockRefreshTokenRepo.Verify(repo => repo.DeleteAsync(It.IsAny<RefreshTokens>()), Times.Never);
        }
    }
}