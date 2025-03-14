using ClaimRequest.DAL.Data.Responses.Otp;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IOtpService
    {
        Task CreateOtpEntity(string email, string otp);
        Task<ValidateOtpResponse> ValidateOtp(string email, string otp);
    }
}
