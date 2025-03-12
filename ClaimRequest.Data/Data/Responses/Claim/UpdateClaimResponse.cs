namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class UpdateClaimResponse
    {
        public Guid ClaimId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    }
}
