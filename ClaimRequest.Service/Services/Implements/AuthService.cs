using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Exceptions;
using ClaimRequest.DAL.Data.Requests.Auth;
using ClaimRequest.DAL.Data.Responses.Auth;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClaimRequest.BLL.Services.Implements
{
    public class AuthService : BaseService<AuthService>, IAuthService
    {
        private readonly JwtUtil _jwtUtil;
        private readonly IOtpService _otpService;
        private readonly IRefreshTokensService _refreshTokensService;

        public AuthService(
             IUnitOfWork<ClaimRequestDbContext> unitOfWork,
             ILogger<AuthService> logger,
             IMapper mapper,
             IHttpContextAccessor httpContextAccessor,
             JwtUtil jwtUtil,
            IOtpService otpService,
            IRefreshTokensService refreshTokensService)
             : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _jwtUtil = jwtUtil;
            _otpService = otpService;
            _refreshTokensService = refreshTokensService;
        }

        public async Task<LoginResponse> Login(LoginRequest loginRequest)
        {
            Expression<Func<Staff, bool>> searchEmailAddress = p => p.Email.Equals(loginRequest.Email);

            var staff = (await _unitOfWork.GetRepository<Staff>().SingleOrDefaultAsync(predicate: searchEmailAddress))
                .ValidateExists(customMessage: $"User with email {loginRequest.Email} not found.");

            bool passwordVerify = await PasswordUtil.VerifyPassword(loginRequest.Password, staff.Password)
                ? true
                : throw new WrongPasswordException("Invalid password");

            DateTime? lastChangePassword = staff.LastChangePassword;
            bool isPasswordExpired = lastChangePassword == null || lastChangePassword <= DateTime.UtcNow.AddMonths(-3);

            LoginResponse loginResponse = new LoginResponse(staff);
            loginResponse.IsPasswordExpired = isPasswordExpired;

            Tuple<string, Guid> guidSecurityClaim = new Tuple<string, Guid>("StaffId", staff.Id);

            if (isPasswordExpired)
            {
                // Tạo resetToken cho resetPasswordOnly
                var resetToken = _jwtUtil.GenerateJwtToken(staff, guidSecurityClaim, true);

                // Ném exception với resetToken trong ExceptionMessage
                throw new PasswordExpiredException(resetToken);
            }

            // Token bình thường nếu mật khẩu không hết hạn
            var token = _jwtUtil.GenerateJwtToken(staff, guidSecurityClaim, false);
            var refreshToken = await _refreshTokensService.GenerateAndStoreRefreshToken(staff.Id);

            loginResponse.AccessToken = token;
            loginResponse.RefreshToken = refreshToken; // Thêm refresh token vào response
            return loginResponse;
        }

        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
        {
            var staffRepository = _unitOfWork.GetRepository<Staff>();

            var staff = (await staffRepository.SingleOrDefaultAsync(
                predicate: s => s.Email == forgotPasswordRequest.Email && s.IsActive
            )).ValidateExists(customMessage: "Staff not found or inactive.");

            var otpValidationResult = await _otpService.ValidateOtp(forgotPasswordRequest.Email, forgotPasswordRequest.Otp);
            if (!otpValidationResult.Success)
            {
                throw new OtpValidationException("Invalid OTP.", otpValidationResult.AttemptsLeft);
            }

            staff.Password = await PasswordUtil.HashPassword(forgotPasswordRequest.NewPassword);
            staff.LastChangePassword = DateTime.UtcNow;
            staffRepository.UpdateAsync(staff);
            await _unitOfWork.CommitAsync();

            return new ForgotPasswordResponse
            {
                Success = true,
                AttemptsLeft = otpValidationResult.AttemptsLeft
            };
        }

        public async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            var staffRepository = _unitOfWork.GetRepository<Staff>();
            var staff = (await staffRepository.SingleOrDefaultAsync(
                predicate: s => s.Email == changePasswordRequest.Email && s.IsActive
            )).ValidateExists(customMessage: "Staff not found or inactive.");

            var otpRepository = _unitOfWork.GetRepository<Otp>();
            var otpEntity = await otpRepository.SingleOrDefaultAsync(
                predicate: o => o.Email == changePasswordRequest.Email
            );

            int attemptsLeft = otpEntity?.AttemptLeft ?? 0;

            bool oldPasswordVerify = await PasswordUtil.VerifyPassword(changePasswordRequest.OldPassword, staff.Password);
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

                    await _unitOfWork.CommitAsync();
                }

                throw new OtpValidationException("Invalid old password.", attemptsLeft);
            }

            if (changePasswordRequest.NewPassword == changePasswordRequest.OldPassword)
            {
                throw new InvalidOperationException("New password must be different from the old password.");
            }

            var otpValidationResult = await _otpService.ValidateOtp(changePasswordRequest.Email, changePasswordRequest.Otp);
            if (!otpValidationResult.Success)
            {
                throw new OtpValidationException("Invalid OTP.", otpValidationResult.AttemptsLeft);
            }

            staff.Password = await PasswordUtil.HashPassword(changePasswordRequest.NewPassword);
            staff.LastChangePassword = DateTime.UtcNow;

            staffRepository.UpdateAsync(staff);

            await _unitOfWork.CommitAsync();

            return new ChangePasswordResponse
            {
                Success = true,
                AttemptsLeft = otpValidationResult.AttemptsLeft
            };
        }
    }
}
