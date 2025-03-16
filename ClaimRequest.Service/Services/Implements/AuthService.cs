using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
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

        public AuthService(
             IUnitOfWork<ClaimRequestDbContext> unitOfWork,
             ILogger<AuthService> logger,
             IMapper mapper,
             IHttpContextAccessor httpContextAccessor,
             JwtUtil jwtUtil,
            IOtpService otpService)
             : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _jwtUtil = jwtUtil;
            _otpService = otpService;
        }

        public async Task<LoginResponse> Login(LoginRequest loginRequest)
        {
            Expression<Func<Staff, bool>> searchEmailAddress = p => p.Email.Equals(loginRequest.Email);

            var staff = (await _unitOfWork.GetRepository<Staff>().SingleOrDefaultAsync(predicate: searchEmailAddress))
                .ValidateExists(customMessage: $"User with email {loginRequest.Email} not found.");

            bool passwordVerify = await PasswordUtil.VerifyPassword(loginRequest.Password, staff.Password)
                ? true
                : throw new UnauthorizedAccessException("Invalid password");

            LoginResponse loginResponse = new LoginResponse(staff);
            Tuple<string, Guid> guidSecurityClaim = new Tuple<string, Guid>("StaffId", staff.Id);
            var token = _jwtUtil.GenerateJwtToken(staff, guidSecurityClaim);
            loginResponse.AccessToken = token;
            return loginResponse;
        }

        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
        {
            try
            {
                var staffRepository = _unitOfWork.GetRepository<Staff>();

                var staff = await staffRepository.SingleOrDefaultAsync(
                    predicate: s => s.Email == forgotPasswordRequest.Email && s.IsActive
                );

                if (staff == null)
                {
                    throw new Exception("Staff not found or inactive.");
                }

                var otpValidationResult = await _otpService.ValidateOtp(forgotPasswordRequest.Email, forgotPasswordRequest.Otp);
                if (!otpValidationResult.Success)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        AttemptsLeft = otpValidationResult.AttemptsLeft
                    };
                }

                staff.Password = await PasswordUtil.HashPassword(forgotPasswordRequest.NewPassword);

                staffRepository.UpdateAsync(staff);

                await _unitOfWork.CommitAsync();

                return new ForgotPasswordResponse
                {
                    Success = true,
                    AttemptsLeft = otpValidationResult.AttemptsLeft
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            try
            {
                var staffRepository = _unitOfWork.GetRepository<Staff>();
                var staff = await staffRepository.SingleOrDefaultAsync(
                    predicate: s => s.Email == changePasswordRequest.Email && s.IsActive
                );

                if (staff == null)
                {
                    throw new Exception("Staff not found or inactive.");
                }

                bool oldPasswordVerify = await PasswordUtil.VerifyPassword(changePasswordRequest.OldPassword, staff.Password);
                if (!oldPasswordVerify)
                {
                    var otpRepository = _unitOfWork.GetRepository<Otp>();
                    var otpEntity = await otpRepository.SingleOrDefaultAsync(
                        predicate: o => o.Email == changePasswordRequest.Email
                    );

                    int attemptsLeft = 0;
                    if (otpEntity != null)
                    {
                        if (otpEntity.AttemptLeft > 0)
                        {
                            otpEntity.AttemptLeft -= 1;
                            attemptsLeft = otpEntity.AttemptLeft;
                            otpRepository.UpdateAsync(otpEntity);
                            await _unitOfWork.CommitAsync();
                        }
                        else
                        {
                            attemptsLeft = otpEntity.AttemptLeft;
                        }
                    }

                    return new ChangePasswordResponse
                    {
                        Success = false,
                        AttemptsLeft = attemptsLeft,
                        Message = "Invalid old password"
                    };
                }

                if (changePasswordRequest.NewPassword == changePasswordRequest.OldPassword)
                {
                    throw new Exception("New password must be different from the old password.");
                }

                var otpValidationResult = await _otpService.ValidateOtp(changePasswordRequest.Email, changePasswordRequest.Otp);
                if (!otpValidationResult.Success)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        AttemptsLeft = otpValidationResult.AttemptsLeft,
                        Message = otpValidationResult.Message
                    };
                }

                staff.Password = await PasswordUtil.HashPassword(changePasswordRequest.NewPassword);

                staffRepository.UpdateAsync(staff);

                await _unitOfWork.CommitAsync();

                return new ChangePasswordResponse
                {
                    Success = true,
                    AttemptsLeft = otpValidationResult.AttemptsLeft,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password: {Message}", ex.Message);
                throw;
            }
        }
    }
}
