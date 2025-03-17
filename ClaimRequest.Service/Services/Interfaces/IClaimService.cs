﻿using System.Security.Claims;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Data.Requests.Claim;
using ClaimRequest.DAL.Data.Responses.Claim;
using Claim = ClaimRequest.DAL.Data.Entities.Claim;

namespace ClaimRequest.BLL.Services.Interfaces
{
    public interface IClaimService
    {
        Task<CreateClaimResponse> CreateClaim(CreateClaimRequest createClaimRequest);
        Task<UpdateClaimResponse> UpdateClaim(Guid claimId, UpdateClaimRequest updateClaimRequest);
        Task<CancelClaimResponse> CancelClaim(CancelClaimRequest cancelClaimRequest);
        Task<PagingResponse<ViewClaimResponse>> GetClaims(int pageNumber = 1, int pageSize = 20, ClaimStatus? status = null, string? viewMode = null);

        Task<ViewClaimResponse> GetClaimById(Guid id);

        Task<Claim> AddEmailInfo(Guid id);
        Task<RejectClaimResponse> RejectClaim(Guid Id, RejectClaimRequest rejectClaimRequest);
        Task<MemoryStream> DownloadClaimAsync(DownloadClaimRequest downloadClaimRequest);
        Task<bool> ApproveClaim(ClaimsPrincipal user, Guid id);
        Task<ReturnClaimResponse> ReturnClaim(Guid id, ReturnClaimRequest returnClaimRequest);
        Task<bool> SubmitClaim(Guid id);
        Task<bool> PaidClaim(Guid id, Guid financeId);

    }
}
