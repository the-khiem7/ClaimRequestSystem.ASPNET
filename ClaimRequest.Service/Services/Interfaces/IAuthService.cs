using ClaimRequest.DAL.Data.Requests;
using ClaimRequest.DAL.Data.Responses;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest loginRequest);
    }
}
