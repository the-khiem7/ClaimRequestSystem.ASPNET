using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class UpdateClaimResponse
    {
        public ClaimType ClaimType { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public decimal Amount { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    }
}

