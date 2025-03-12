using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IClaimService
    {
        Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest);
        Task<UpdateClaimResponse> UpdateClaim(Guid Id, UpdateClaimRequest updateClaimRequest);
        Task<CancelClaimResponse> CancelClaim(Guid claimId, CancelClaimRequest cancelClaimRequest);
        Task<PagingResponse<ViewClaimResponse>> GetClaims(int pageNumber = 1, int pageSize = 20, ClaimStatus? status = null);

        Task<ViewClaimResponse> GetClaimById(Guid id);
        Task<RejectClaimResponse> RejectClaim(Guid Id, RejectClaimRequest rejectClaimRequest);
        Task<MemoryStream> DownloadClaimAsync(DownloadClaimRequest downloadClaimRequest);
        Task<bool> ApproveClaim(Guid approverId, Guid id);
        Task<ReturnClaimResponse> ReturnClaim(Guid id, ReturnClaimRequest returnClaimRequest);

        Task<bool> PaidClaim(Guid id, Guid financeId);

    }
}
