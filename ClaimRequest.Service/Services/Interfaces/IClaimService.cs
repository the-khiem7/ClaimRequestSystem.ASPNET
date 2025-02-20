using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IClaimService
    {
        Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest);
        Task<CancelClaimResponse> CancelClaim(CancelClaimRequest cancelClaimRequest);   
        Task<IEnumerable<ViewClaimResponse>> GetClaims(ClaimStatus? status);
        Task<ViewClaimResponse> GetClaimById(Guid id);
        Task<RejectClaimResponse> RejectClaim(Guid Id, RejectClaimRequest rejectClaimRequest);
        Task<ApproveClaimResponse> ApproveClaim(Guid Id, ApproveClaimRequest approveClaimRequest);
        Task<PaidClaimResponse> PaidClaim(Guid Id, PaidClaimRequest request);

        Task<MemoryStream> DownloadClaimAsync(DownloadClaimRequest downloadClaimRequest);
        
    }
}
