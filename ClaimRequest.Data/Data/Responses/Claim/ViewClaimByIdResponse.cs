using ClaimRequest.DAL.Data.Entities;

namespace ClaimRequest.DAL.Data.Responses.Claim
{
    public class ViewClaimByIdResponse
    {
        // Claim details
        public Guid Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public decimal Amount { get; set; }
        public ClaimType ClaimType { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public ClaimStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }

        // Related entities
        public StaffDetails Claimer { get; set; }
        public ProjectDetails Project { get; set; }
        public StaffDetails ProjectManager { get; set; }
        public StaffDetails Finance { get; set; }

        public class StaffDetails
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public SystemRole SystemRole { get; set; }
            public Department Department { get; set; }
            public decimal Salary { get; set; }
            public bool IsActive { get; set; }
            public string Avatar { get; set; }
            public DateTime? LastChangePassword { get; set; }
        }

        public class ProjectDetails
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public ProjectStatus Status { get; set; }
            public DateOnly StartDate { get; set; }
            public DateOnly EndDate { get; set; }
            public decimal Budget { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
