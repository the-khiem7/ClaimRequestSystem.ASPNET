using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class CreateClaimResponse
    {
        public ClaimType ClaimType { get; set; }

        public string Name { get; set; }

        public string Remark { get; set; }

        public decimal Amount { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public decimal TotalWorkingHours { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public class Project
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public ProjectStatus Status { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
        public Guid ClaimerId { get; set; }
    }
}
