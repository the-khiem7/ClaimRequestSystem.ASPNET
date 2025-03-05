using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Data.Responses.Email;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(SendMailRequest request);
        Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request);
    }
}
