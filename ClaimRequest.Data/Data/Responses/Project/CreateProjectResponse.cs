using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.Responses.Staff;

namespace ClaimRequest.DAL.Data.Responses.Project
{
    public class CreateProjectResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ProjectStatus Status { get; set; }

        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public DateOnly? EndDate { get; set; }

        public decimal Budget { get; set; }

        public CreateStaffResponse ProjectManager { get; set; }

        public CreateStaffResponse FinanceStaff { get; set; }

        public List<ProjectStaffResponse> ProjectStaffs { get; set; }
    }
}
