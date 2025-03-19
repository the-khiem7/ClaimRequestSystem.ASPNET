using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ViewClaimResponse
    {
        public Guid Id { get; set; }
        public string StaffName { get; set; }
        public string ProjectName { get; set; }
        public DateOnly ProjectStartDate { get; set; }
        public DateOnly ProjectEndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public decimal Amount { get; set; }
        public ClaimStatus status { get; set; }
        public Guid? FinanceId { get; set; }
    }
}