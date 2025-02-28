using ClaimRequest.DAL.Data.Requests.Email;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(SendMailRequest request);
    }
}
