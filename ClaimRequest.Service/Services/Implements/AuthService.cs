using System.Linq.Expressions;
using AutoMapper;
using ClaimRequest.BLL.Extension;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests;
using ClaimRequest.DAL.Data.Responses;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ClaimRequest.BLL.Services.Implements
{
    public class AuthService : BaseService<AuthService>, IAuthService
    {
        public AuthService(
             IUnitOfWork<ClaimRequestDbContext> unitOfWork,
             ILogger<AuthService> logger,
             IMapper mapper,
             IHttpContextAccessor httpContextAccessor)
             : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }

        public async Task<LoginResponse> Login(LoginRequest loginRequest)
        {
            Expression<Func<Staff, bool>> searchEmailAddress = p => p.Email.Equals(loginRequest.Email);

            var staff = (await _unitOfWork.GetRepository<Staff>()
                .SingleOrDefaultAsync(predicate: searchEmailAddress))
                .ValidateExists(customMessage: $"User with email {loginRequest.Email} not found.");

            bool passwordVerify = await PasswordUtil.VerifyPassword(loginRequest.Password, staff.Password)
                ? true
                : throw new UnauthorizedAccessException("Invalid password");

            var loginResponse = new LoginResponse(staff);
            return loginResponse;
        }
    }
}
