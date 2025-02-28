using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ApproveClaimResponse
    {
        public Guid Id { get; set; }
        public string Remark { get; set; }
        public ClaimStatus Status { get; set; }
        public Guid ClaimerId { get; set; }
        public DateTime UpdateAt { get; set; }
        public Guid ApproverId { get; set; }
    }
}
