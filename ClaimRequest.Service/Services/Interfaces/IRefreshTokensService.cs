
namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IRefreshTokensService
    {
        Task<string> GenerateAndStoreRefreshToken(Guid userId);
        Task<string> RefreshAccessToken(string refreshToken);
        Task<bool> DeleteRefreshToken(string refreshToken);
    }
}
