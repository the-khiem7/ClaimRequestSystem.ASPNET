using System.Security.Cryptography;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;

namespace ClaimRequest.BLL.Services.Implements
{
    public class RefreshTokensService : IRefreshTokensService
    {
        private readonly IUnitOfWork<ClaimRequestDbContext> _unitOfWork;
        private readonly IJwtUtil _jwtUtil;

        public RefreshTokensService(IUnitOfWork<ClaimRequestDbContext> unitOfWork, IJwtUtil jwtUtil)
        {
            _unitOfWork = unitOfWork;
            _jwtUtil = jwtUtil;
        }

        public async Task<string> GenerateAndStoreRefreshToken(Guid userId)
        {
            return await _unitOfWork.ProcessInTransactionAsync(async () =>
            {
                var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                var refreshTokenRepo = _unitOfWork.GetRepository<RefreshTokens>();
                var newToken = new RefreshTokens
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = refreshToken,
                    CreateAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await refreshTokenRepo.InsertAsync(newToken);
                return refreshToken;
        });
        }

        public async Task<string> RefreshAccessToken(string refreshToken)
        {
            return await _unitOfWork.ProcessInTransactionAsync(async () =>
            {
                var refreshTokenRepo = _unitOfWork.GetRepository<RefreshTokens>();
                var storedToken = await refreshTokenRepo.SingleOrDefaultAsync<RefreshTokens>(t => t, t => t.Token == refreshToken);

                if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    throw new Exception("Invalid or expired refresh token");
                }

                var staff = await _unitOfWork.GetRepository<Staff>().GetByIdAsync(storedToken.UserId);
                if (staff == null) throw new Exception("Staff not found");
                Tuple<string, Guid> guidSecurityClaim = new Tuple<string, Guid>("StaffId", staff.Id);

                return _jwtUtil.GenerateJwtToken(staff, guidSecurityClaim, false);
        });
        }

        public async Task<bool> DeleteRefreshToken(string refreshToken)
        {
            return await _unitOfWork.ProcessInTransactionAsync(async () =>
            {
                var refreshTokenRepo = _unitOfWork.GetRepository<RefreshTokens>();
                var tokenToDelete = await refreshTokenRepo.SingleOrDefaultAsync(
                    predicate: t => t.Token == refreshToken);
                
                if (tokenToDelete != null)
                {
                    refreshTokenRepo.DeleteAsync(tokenToDelete);
                    return true;
                }
                return false;
            });
        }
    }
}
