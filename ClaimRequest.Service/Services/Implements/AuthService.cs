using System.Linq.Expressions;
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
        public async Task<bool> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
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
                    throw new Exception("Invalid or expired OTP.");
                }

                staff.Password = await PasswordUtil.HashPassword(forgotPasswordRequest.NewPassword);
  
                staffRepository.UpdateAsync(staff);
                
                await _unitOfWork.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password: {Message}", ex.Message);
                throw;
            }
        }
    }
}
