using ClaimRequest.DAL.Data.Requests;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(Guid claimId);
        Task SendClaimApprovedEmail(Guid claimId);
        Task SendClaimReturnedEmail(Guid claimId);
        Task SendClaimSubmittedEmail(Guid claimId);
        Task SendManagerApprovedEmail(Guid approverId, Guid claimId);
        Task SendEmailReminderAsync();
    }
}
