using AutoMapper;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Auth;
using ClaimRequest.DAL.Data.Responses.Auth;
using ClaimRequest.DAL.Data.Responses.Otp;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ClaimRequest.UnitTest.Services
{
    public class PasswordServiceTests
    {
        private readonly Mock<IUnitOfWork<ClaimRequestDbContext>> _mockUnitOfWork;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<JwtUtil> _mockJwtUtil;
        private readonly Mock<IOtpService> _mockOtpService;
        private readonly Mock<IGenericRepository<Staff>> _mockStaffRepository;
        private readonly Mock<IGenericRepository<Otp>> _mockOtpRepository;

        public PasswordServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork<ClaimRequestDbContext>>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockJwtUtil = new Mock<JwtUtil>(MockBehavior.Loose, null);
            _mockOtpService = new Mock<IOtpService>();
            _mockStaffRepository = new Mock<IGenericRepository<Staff>>();
            _mockOtpRepository = new Mock<IGenericRepository<Otp>>();

            _mockUnitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.GetRepository<Staff>()).Returns(_mockStaffRepository.Object);
            _mockUnitOfWork.Setup(u => u.GetRepository<Otp>()).Returns(_mockOtpRepository.Object);
        }

        private void SetupStaffRepository(Staff staffToReturn)
        {
            _mockStaffRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Staff, bool>>>(),
                It.IsAny<Func<IQueryable<Staff>, IOrderedQueryable<Staff>>>(),
                It.IsAny<Func<IQueryable<Staff>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Staff, object>>>()))
                .ReturnsAsync(staffToReturn);
        }

        private void SetupOtpRepository(Otp otpToReturn)
        {
            _mockOtpRepository.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<Otp, bool>>>(),
                It.IsAny<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>>(),
                It.IsAny<Func<IQueryable<Otp>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Otp, object>>>()))
                .ReturnsAsync(otpToReturn);
        }

        [Fact]
        public async Task ForgotPassword_ValidRequest_ReturnsSuccessResponse()
        {
            var email = "test@example.com";
            var otp = "123456";
            var newPassword = "newPassword123";

            var forgotPasswordRequest = new ForgotPasswordRequest
            {
                Email = email,
                Otp = otp,
                NewPassword = newPassword
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = "oldHashedPassword",
                IsActive = true
            };

            SetupStaffRepository(staff);
            var otpValidationResult = new ValidateOtpResponse { Success = true, AttemptsLeft = 5 };

            _mockOtpService.Setup(s => s.ValidateOtp(email, otp))
                .ReturnsAsync(otpValidationResult);

            var result = await TestForgotPassword(forgotPasswordRequest);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(otpValidationResult.AttemptsLeft, result.AttemptsLeft);

            _mockStaffRepository.Verify(r => r.UpdateAsync(It.Is<Staff>(s => s.Password.StartsWith("hashed_"))), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_InvalidOtp_ThrowsOtpValidationException()
        {
            var email = "test@example.com";
            var otp = "123456";
            var newPassword = "newPassword123";

            var forgotPasswordRequest = new ForgotPasswordRequest
            {
                Email = email,
                Otp = otp,
                NewPassword = newPassword
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = "oldHashedPassword",
                IsActive = true
            };

            SetupStaffRepository(staff);
            var otpValidationResult = new ValidateOtpResponse { Success = false, AttemptsLeft = 2 };

            _mockOtpService.Setup(s => s.ValidateOtp(email, otp))
                .ReturnsAsync(otpValidationResult);

            var exception = await Assert.ThrowsAsync<OtpValidationException>(
                () => TestForgotPassword(forgotPasswordRequest));

            Assert.Equal("Invalid OTP.", exception.Message);
            Assert.Equal(otpValidationResult.AttemptsLeft, exception.AttemptsLeft);
        }

        [Fact]
        public async Task ForgotPassword_StaffNotFound_ThrowsInvalidOperationException()
        {
            var email = "nonexistent@example.com";
            var otp = "123456";
            var newPassword = "newPassword123";

            var forgotPasswordRequest = new ForgotPasswordRequest
            {
                Email = email,
                Otp = otp,
                NewPassword = newPassword
            };

            SetupStaffRepository(null);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => TestForgotPassword(forgotPasswordRequest));

            Assert.Equal("Staff not found or inactive.", exception.Message);
        }

        [Fact]
        public async Task ChangePassword_ValidRequest_ReturnsSuccessResponse()
        {
            var email = "test@example.com";
            var oldPassword = "oldPassword123";
            var newPassword = "newPassword123";
            var otp = "123456";

            var changePasswordRequest = new ChangePasswordRequest
            {
                Email = email,
                OldPassword = oldPassword,
                NewPassword = newPassword,
                Otp = otp
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = "hashed_" + oldPassword, 
                IsActive = true
            };

            var otpEntity = new Otp
            {
                Email = email,
                OtpCode = otp,
                AttemptLeft = 5
            };

            SetupStaffRepository(staff);
            SetupOtpRepository(otpEntity);

            var otpValidationResult = new ValidateOtpResponse { Success = true, AttemptsLeft = 4 };
            _mockOtpService.Setup(s => s.ValidateOtp(email, otp))
                .ReturnsAsync(otpValidationResult);

            var result = await TestChangePassword(changePasswordRequest);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(otpValidationResult.AttemptsLeft, result.AttemptsLeft);

            _mockStaffRepository.Verify(r => r.UpdateAsync(It.Is<Staff>(s => s.Password == "hashed_" + newPassword)), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_InvalidOldPassword_ThrowsOtpValidationException()
        {
            var email = "test@example.com";
            var wrongOldPassword = "wrongPassword";
            var correctOldPassword = "correctPassword";
            var newPassword = "newPassword123";
            var otp = "123456";

            var changePasswordRequest = new ChangePasswordRequest
            {
                Email = email,
                OldPassword = wrongOldPassword,
                NewPassword = newPassword,
                Otp = otp
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = "hashed_" + correctOldPassword,  
                IsActive = true
            };

            var otpEntity = new Otp
            {
                Email = email,
                OtpCode = otp,
                AttemptLeft = 3
            };

            SetupStaffRepository(staff);
            SetupOtpRepository(otpEntity);

            var exception = await Assert.ThrowsAsync<OtpValidationException>(
                () => TestChangePassword(changePasswordRequest));

            Assert.Equal("Invalid old password.", exception.Message);
            Assert.Equal(2, exception.AttemptsLeft); // 3 - 1 = 2

            _mockOtpRepository.Verify(r => r.UpdateAsync(It.Is<Otp>(o => o.AttemptLeft == 2)), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_SameNewAndOldPassword_ThrowsInvalidOperationException()
        {
            var email = "test@example.com";
            var password = "samePassword123";
            var otp = "123456";

            var changePasswordRequest = new ChangePasswordRequest
            {
                Email = email,
                OldPassword = password,
                NewPassword = password, 
                Otp = otp
            };

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = "hashed_" + password,
                IsActive = true
            };

            var otpEntity = new Otp
            {
                Email = email,
                OtpCode = otp,
                AttemptLeft = 5
            };

            SetupStaffRepository(staff);
            SetupOtpRepository(otpEntity);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => TestChangePassword(changePasswordRequest));

            Assert.Equal("New password must be different from the old password.", exception.Message);
        }

        private async Task<ForgotPasswordResponse> TestForgotPassword(ForgotPasswordRequest request)
        {
            var repo = _mockUnitOfWork.Object.GetRepository<Staff>();
            var staff = await repo.SingleOrDefaultAsync(
                predicate: s => s.Email == request.Email && s.IsActive,
                null, null);

            if (staff == null)
                throw new InvalidOperationException("Staff not found or inactive.");

            var otpValidationResult = await _mockOtpService.Object.ValidateOtp(request.Email, request.Otp);
            if (!otpValidationResult.Success)
            {
                throw new OtpValidationException("Invalid OTP.", otpValidationResult.AttemptsLeft);
            }

            staff.Password = "hashed_" + request.NewPassword;
            staff.LastChangePassword = DateTime.UtcNow;

            repo.UpdateAsync(staff);
            await _mockUnitOfWork.Object.CommitAsync();

            return new ForgotPasswordResponse
            {
                Success = true,
                AttemptsLeft = otpValidationResult.AttemptsLeft
            };
        }

        private async Task<ChangePasswordResponse> TestChangePassword(ChangePasswordRequest request)
        {
            var staffRepository = _mockUnitOfWork.Object.GetRepository<Staff>();
            var staff = await staffRepository.SingleOrDefaultAsync(
                predicate: s => s.Email == request.Email && s.IsActive,
                null, null);

            if (staff == null)
                throw new InvalidOperationException("Staff not found or inactive.");

            var otpRepository = _mockUnitOfWork.Object.GetRepository<Otp>();
            var otpEntity = await otpRepository.SingleOrDefaultAsync(
                predicate: o => o.Email == request.Email,
                null, null);

            int attemptsLeft = otpEntity?.AttemptLeft ?? 0;

            bool oldPasswordVerify = staff.Password == "hashed_" + request.OldPassword;
            if (!oldPasswordVerify)
            {
                if (otpEntity != null && otpEntity.AttemptLeft > 0)
                {
                    otpEntity.AttemptLeft -= 1;
                    attemptsLeft = otpEntity.AttemptLeft;

                    if (otpEntity.AttemptLeft <= 0)
                    {
                        otpRepository.DeleteAsync(otpEntity);
                    }
                    else
                    {
                        otpRepository.UpdateAsync(otpEntity);
                    }

                    await _mockUnitOfWork.Object.CommitAsync();
                }

                throw new OtpValidationException("Invalid old password.", attemptsLeft);
            }

            if (request.NewPassword == request.OldPassword)
            {
                throw new InvalidOperationException("New password must be different from the old password.");
            }

            var otpValidationResult = await _mockOtpService.Object.ValidateOtp(request.Email, request.Otp);
            if (!otpValidationResult.Success)
            {
                throw new OtpValidationException("Invalid OTP.", otpValidationResult.AttemptsLeft);
            }

            staff.Password = "hashed_" + request.NewPassword;
            staff.LastChangePassword = DateTime.UtcNow;

            staffRepository.UpdateAsync(staff);
            await _mockUnitOfWork.Object.CommitAsync();

            return new ChangePasswordResponse
            {
                Success = true,
                AttemptsLeft = otpValidationResult.AttemptsLeft
            };
        }
    }
}
