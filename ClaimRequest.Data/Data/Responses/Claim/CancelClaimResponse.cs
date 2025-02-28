namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class CancelClaimResponse
    {
        public Guid ClaimId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; } = "Cancelled";
        public string Remark { get; set; }
        public decimal Amount { get; set; }
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
        public Guid ClaimerId { get; set; }
    }
}
