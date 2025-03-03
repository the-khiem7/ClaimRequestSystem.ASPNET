using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using Claim = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IClaimService
    {
        Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest);
        Task<UpdateClaimResponse> UpdateClaim(Guid Id, UpdateClaimRequest updateClaimRequest);
        Task<CancelClaimResponse> CancelClaim(CancelClaimRequest cancelClaimRequest);
        Task<IEnumerable<ViewClaimResponse>> GetClaims(ClaimStatus? status);
        Task<Claim> GetClaimById(Guid id);
        Task<RejectClaimResponse> RejectClaim(Guid Id, RejectClaimRequest rejectClaimRequest);
        Task<MemoryStream> DownloadClaimAsync(DownloadClaimRequest downloadClaimRequest);
        Task<ApproveClaimResponse> ApproveClaim(Guid id, ApproveClaimRequest approveClaimRequest);
        Task<ReturnClaimResponse> ReturnClaim(Guid id, ReturnClaimRequest returnClaimRequest);
    }
}
