using ClaimRequest.DAL.Data.Requests;
using ClaimRequest.DAL.Data.Requests.Email;
using ClaimRequest.DAL.Data.Responses.Email;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task<SendOtpEmailResponse> SendOtpEmailAsync(SendOtpEmailRequest request);
        Task SendClaimApprovedEmail(Guid claimId);
        Task SendClaimReturnedEmail(Guid claimId);
        Task SendClaimSubmittedEmail(Guid claimId);
        Task SendManagerApprovedEmail(Guid approverId, Guid claimId);
    }
}
