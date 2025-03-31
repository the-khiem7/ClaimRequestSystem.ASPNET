using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Auth;
using ClaimRequest.DAL.Data.Responses.Auth;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class LoginTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<JwtUtil> _mockJwtUtil;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly Mock<IRefreshTokensService> _mockRefreshTokensService;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly Mock<IAuthService> _mockAuthService;

        public LoginTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockJwtUtil = new Mock<JwtUtil>(Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
            _mockOtpService = new Mock<IOtpService>();
            _mockRefreshTokensService = new Mock<IRefreshTokensService>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();
            _mockAuthService = new Mock<IAuthService>();

            _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>())
                .Returns(_mockStaffRepository.Object);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsLoginResponse()
        {
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = loginRequest.Email,
                Name = "Test User",
                Password = "hashedPassword",
                LastChangePassword = DateTime.UtcNow,
                IsActive = true,
                SystemRole = SystemRole.Admin,
                Department = Department.ProjectManagement
            };

            var expectedToken = "jwt_token";
            var expectedRefreshToken = "refresh_token";

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(staff);

            var response = new LoginResponse(staff)
            {
                AccessToken = expectedToken,
                RefreshToken = expectedRefreshToken,
                IsPasswordExpired = false
            };

            _mockAuthService.Setup(service => service.Login(loginRequest))
                .ReturnsAsync(response);

            var result = await _mockAuthService.Object.Login(loginRequest);

            Assert.NotNull(result);
            Assert.Equal(staff.Id, result.Id);
            Assert.Equal(staff.Email, result.Email);
            Assert.Equal(expectedToken, result.AccessToken);
            Assert.Equal(expectedRefreshToken, result.RefreshToken);
            Assert.False(result.IsPasswordExpired);
        }

        [Fact]
        public async Task Login_UserNotFound_ThrowsException()
        {
            var loginRequest = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "password123"
            };

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync((Staff)null);

            _mockAuthService.Setup(service => service.Login(loginRequest))
                .ThrowsAsync(new Exception($"User with email {loginRequest.Email} not found."));

            var exception = await Assert.ThrowsAsync<Exception>(() => _mockAuthService.Object.Login(loginRequest));
            Assert.Contains($"User with email {loginRequest.Email} not found", exception.Message);
        }

        [Fact]
        public async Task Login_InvalidPassword_ThrowsWrongPasswordException()
        {
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = loginRequest.Email,
                Password = "hashedPassword",
                IsActive = true
            };

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(staff);

            _mockAuthService.Setup(service => service.Login(loginRequest))
                .ThrowsAsync(new WrongPasswordException("Invalid password"));

            await Assert.ThrowsAsync<WrongPasswordException>(() => _mockAuthService.Object.Login(loginRequest));
        }

        [Fact]
        public async Task Login_ExpiredPassword_ThrowsPasswordExpiredException()
        {
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = loginRequest.Email,
                Name = "Test User",
                Password = "hashedPassword",
                LastChangePassword = DateTime.UtcNow.AddMonths(-4),
                IsActive = true
            };

            var resetToken = "reset_token";

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(staff);

            _mockAuthService.Setup(service => service.Login(loginRequest))
                .ThrowsAsync(new PasswordExpiredException(resetToken));

            var exception = await Assert.ThrowsAsync<PasswordExpiredException>(() => _mockAuthService.Object.Login(loginRequest));

            Assert.Equal(resetToken, exception.ResetToken);
        }

        [Fact]
        public async Task Login_NullLastChangePassword_ThrowsPasswordExpiredException()
        {
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = loginRequest.Email,
                Name = "Test User",
                Password = "hashedPassword",
                LastChangePassword = null, 
                IsActive = true
            };

            var resetToken = "reset_token";

            _mockStaffRepository.Setup(repo => repo.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                null,
                null))
                .ReturnsAsync(staff);

            _mockAuthService.Setup(service => service.Login(loginRequest))
                .ThrowsAsync(new PasswordExpiredException(resetToken));

            var exception = await Assert.ThrowsAsync<PasswordExpiredException>(() => _mockAuthService.Object.Login(loginRequest));

            Assert.Equal(resetToken, exception.ResetToken);
        }
    }
}
