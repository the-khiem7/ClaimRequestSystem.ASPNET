using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Data.Responses.Email;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendClaimApprovedEmail(Guid Id);
        Task SendClaimReturnedEmail(Guid Id);
        Task SendClaimSubmittedEmail(Guid Id);
        Task SendManagerApprovedEmail(Guid Id);
        Task SendEmailAsync(string recipientEmail, string subject, string body);
        Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request);

    }
}
