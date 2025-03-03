using ClaimRequest.DAL.Data.Requests.Email;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendClaimApprovedEmail(Guid claimId);
        Task SendClaimReturnedEmail(Guid claimId);
        Task SendClaimSubmittedEmail(Guid claimId);
        Task SendManagerApprovedEmail(Guid claimId);
    }
}
